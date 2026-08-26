import http from "node:http";
import crypto from "node:crypto";

const port = Number.parseInt(process.env.PORT ?? "8080", 10);
const upstreamUrl = process.env.TOKENHUB_URL ?? "https://tokenhub.tencentmaas.com/v1/chat/completions";
const visionModel = process.env.TOKENHUB_VISION_MODEL ?? "deepseek/deepseek-v4-flash-vision-exp";
const textModel = process.env.TOKENHUB_TEXT_MODEL ?? "deepseek-v4-flash";
const apiKey = process.env.TOKENHUB_API_KEY ?? "";
const requestLimitBytes = Number.parseInt(process.env.REQUEST_LIMIT_BYTES ?? `${15 * 1024 * 1024}`, 10);
const requestsPerWindow = Number.parseInt(process.env.REQUESTS_PER_10_MINUTES ?? "8", 10);
const dailyLimit = Number.parseInt(process.env.DAILY_REQUEST_LIMIT ?? "500", 10);
const timeoutMs = Number.parseInt(process.env.UPSTREAM_TIMEOUT_MS ?? "65000", 10);

const rateBuckets = new Map();
let dailyCount = 0;
let dailyKey = new Date().toISOString().slice(0, 10);

const instruction = `你是中国高校课程表图片结构化器。结合完整课程表图片和 OCR_BLOCKS 中的二维坐标恢复课程。
不要猜测图片中不存在的信息；合并单元格覆盖的时间段只能生成一条课程；正确识别星期列、节次、起止时间、教师、教室和周次。
只返回 JSON，固定结构：
{"schemaVersion":1,"documentType":"weekly_schedule或other","courses":[{"name":"","teacher":"","location":"","dayOfWeek":"Monday到Sunday","startPeriod":0,"endPeriod":0,"startTime":"HH:mm","endTime":"HH:mm","weeks":[]}]}
如果不是个人周课程表，documentType 必须为 other 且 courses 为空。无法确认的可选字段用空字符串或空数组，不得虚构。`;

function json(response, status, value) {
  const body = JSON.stringify(value);
  response.writeHead(status, {
    "Content-Type": "application/json; charset=utf-8",
    "Content-Length": Buffer.byteLength(body),
    "Cache-Control": "no-store",
    "X-Content-Type-Options": "nosniff"
  });
  response.end(body);
}

function clientAddress(request) {
  const forwarded = request.headers["x-forwarded-for"];
  return (Array.isArray(forwarded) ? forwarded[0] : forwarded?.split(",")[0])?.trim()
    ?? request.socket.remoteAddress
    ?? "unknown";
}

function allowRequest(address) {
  const now = Date.now();
  const today = new Date().toISOString().slice(0, 10);
  if (today !== dailyKey) {
    dailyKey = today;
    dailyCount = 0;
  }
  if (dailyCount >= dailyLimit) return false;

  const bucket = rateBuckets.get(address);
  if (!bucket || now - bucket.startedAt >= 600_000) {
    rateBuckets.set(address, { startedAt: now, count: 1 });
    dailyCount++;
    return true;
  }
  if (bucket.count >= requestsPerWindow) return false;
  bucket.count++;
  dailyCount++;
  return true;
}

async function readJson(request) {
  const chunks = [];
  let total = 0;
  for await (const chunk of request) {
    total += chunk.length;
    if (total > requestLimitBytes) throw Object.assign(new Error("请求体超过限制。"), { status: 413 });
    chunks.push(chunk);
  }
  try {
    return JSON.parse(Buffer.concat(chunks).toString("utf8"));
  } catch {
    throw Object.assign(new Error("请求 JSON 无效。"), { status: 400 });
  }
}

function validateInput(body) {
  const mimeType = String(body?.mimeType ?? "");
  const imageBase64 = String(body?.imageBase64 ?? "");
  const ocrLayout = String(body?.ocrLayout ?? "");
  if (!/^image\/(png|jpeg|webp|bmp)$/.test(mimeType))
    throw Object.assign(new Error("图片格式不受支持。"), { status: 400 });
  if (imageBase64.length < 16 || imageBase64.length > 14_000_000 || !/^[A-Za-z0-9+/=\r\n]+$/.test(imageBase64))
    throw Object.assign(new Error("图片数据无效或超过 10 MB。"), { status: 400 });
  if (ocrLayout.length < 10 || ocrLayout.length > 100_000)
    throw Object.assign(new Error("OCR 版面数据无效。"), { status: 400 });
  return { mimeType, imageBase64, ocrLayout };
}

async function callTokenHub(model, messages) {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), timeoutMs);
  try {
    const response = await fetch(upstreamUrl, {
      method: "POST",
      headers: {
        "Authorization": `Bearer ${apiKey}`,
        "Content-Type": "application/json"
      },
      body: JSON.stringify({
        model,
        messages,
        max_tokens: 4096,
        temperature: 0,
        thinking: { type: "disabled" },
        response_format: { type: "json_object" },
        stream: false
      }),
      signal: controller.signal
    });
    const payload = await response.json().catch(() => ({}));
    if (!response.ok)
      throw new Error(`TokenHub ${response.status}: ${payload?.error?.message ?? "调用失败"}`);
    const content = payload?.choices?.[0]?.message?.content;
    if (typeof content !== "string" || !content.trim()) throw new Error("DeepSeek 返回内容为空。");
    return content;
  } finally {
    clearTimeout(timeout);
  }
}

async function recognize(input) {
  const commonText = `${instruction}\n\n${input.ocrLayout}`;
  try {
    return {
      content: await callTokenHub(visionModel, [
        { role: "system", content: instruction },
        {
          role: "user",
          content: [
            { type: "text", text: `请识别这张课程表。以下本地 OCR 坐标仅用于辅助和交叉校验：\n${input.ocrLayout}` },
            { type: "image_url", image_url: { url: `data:${input.mimeType};base64,${input.imageBase64}` } }
          ]
        }
      ]),
      model: visionModel
    };
  } catch (visionError) {
    console.warn(`[gateway] vision fallback: ${visionError.message}`);
    return {
      content: await callTokenHub(textModel, [
        { role: "system", content: instruction },
        { role: "user", content: commonText }
      ]),
      model: textModel
    };
  }
}

const server = http.createServer(async (request, response) => {
  const requestId = crypto.randomUUID();
  if (request.method === "GET" && request.url === "/health")
    return json(response, apiKey ? 200 : 503, { ok: Boolean(apiKey), service: "schedule-ai-gateway" });
  if (request.method !== "POST" || request.url !== "/v1/schedules/recognize")
    return json(response, 404, { error: "not_found", requestId });
  if (!apiKey) return json(response, 503, { error: "服务器尚未配置 TokenHub API Key。", requestId });
  if (!allowRequest(clientAddress(request)))
    return json(response, 429, { error: "请求过于频繁，请稍后再试。", requestId });

  const startedAt = Date.now();
  try {
    const input = validateInput(await readJson(request));
    const result = await recognize(input);
    json(response, 200, {
      content: result.content,
      provider: "Tencent Cloud TokenHub",
      model: result.model,
      elapsedMs: Date.now() - startedAt,
      requestId
    });
    console.info(`[gateway] ${requestId} ok model=${result.model} elapsedMs=${Date.now() - startedAt}`);
  } catch (error) {
    const status = Number.isInteger(error?.status) ? error.status : 502;
    console.error(`[gateway] ${requestId} failed status=${status} message=${error?.message ?? "unknown"}`);
    json(response, status, { error: error?.name === "AbortError" ? "大模型请求超时。" : error?.message ?? "识别失败。", requestId });
  }
});

server.requestTimeout = 80_000;
server.headersTimeout = 10_000;
server.listen(port, "0.0.0.0", () => console.info(`[gateway] listening on ${port}`));
