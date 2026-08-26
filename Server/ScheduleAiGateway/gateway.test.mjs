import test from "node:test";
import assert from "node:assert/strict";
import http from "node:http";
import { spawn } from "node:child_process";

test("gateway keeps provider key server-side and returns DeepSeek JSON", async () => {
  const upstream = http.createServer(async (request, response) => {
    assert.equal(request.headers.authorization, "Bearer test-secret-key");
    const chunks = [];
    for await (const chunk of request) chunks.push(chunk);
    const body = JSON.parse(Buffer.concat(chunks).toString("utf8"));
    assert.equal(body.model, "deepseek/deepseek-v4-flash-vision-exp");
    assert.equal(body.messages[1].content[1].type, "image_url");
    response.writeHead(200, { "Content-Type": "application/json" });
    response.end(JSON.stringify({
      choices: [{ message: { content: "{\"schemaVersion\":1,\"documentType\":\"weekly_schedule\",\"courses\":[]}" } }]
    }));
  });
  await new Promise(resolve => upstream.listen(18081, "127.0.0.1", resolve));

  const gateway = spawn(process.execPath, ["app.mjs"], {
    cwd: new URL(".", import.meta.url),
    env: {
      ...process.env,
      PORT: "18082",
      TOKENHUB_API_KEY: "test-secret-key",
      TOKENHUB_URL: "http://127.0.0.1:18081/v1/chat/completions"
    },
    stdio: ["ignore", "pipe", "pipe"]
  });

  try {
    await waitForHealth();
    const response = await fetch("http://127.0.0.1:18082/v1/schedules/recognize", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({
        mimeType: "image/png",
        imageBase64: Buffer.from("fake-image-contents").toString("base64"),
        ocrLayout: "OCR_BLOCKS:\n[0,0,1,1] 高等数学"
      })
    });
    assert.equal(response.status, 200);
    const body = await response.json();
    assert.equal(body.provider, "Tencent Cloud TokenHub");
    assert.equal(body.model, "deepseek/deepseek-v4-flash-vision-exp");
    assert.match(body.content, /weekly_schedule/);
  } finally {
    gateway.kill();
    await new Promise(resolve => upstream.close(resolve));
  }

  async function waitForHealth() {
    for (let attempt = 0; attempt < 30; attempt++) {
      try {
        const response = await fetch("http://127.0.0.1:18082/health");
        if (response.ok) return;
      } catch { /* Starting. */ }
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    throw new Error("gateway did not start");
  }
});
