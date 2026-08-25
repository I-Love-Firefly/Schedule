#!/usr/bin/env python3
"""Run a reproducible zero-shot timetable extraction benchmark."""

from __future__ import annotations

import argparse
import json
import re
import time
from pathlib import Path

import torch
from PIL import Image
from transformers import AutoConfig, AutoModelForImageTextToText, AutoProcessor


PROMPT = """你是课程表结构化识别器。读取整张图片，只提取学生实际需要上课的课程。
忽略标题、节次轴、星期表头、备注、课程清单和空白单元格。不要猜测图片中不存在的信息。
一个可见课程单元格只能输出一条记录。周次必须保持为一个字符串，绝对不要按每一周或每天展开记录。
若图片不是个人周课程表，documentType 输出 other，courses 输出空数组。
只输出一个合法 JSON 对象，不要 Markdown，不要解释。格式：
{"schemaVersion":1,"documentType":"weekly_schedule|other","courses":[{"name":"课程名","teacher":"教师或空字符串","location":"地点或空字符串","dayOfWeek":"Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday","startPeriod":1,"endPeriod":2,"startTime":"HH:mm或空字符串","endTime":"HH:mm或空字符串","weeks":"周次原文或空字符串"}]}
同一课程在图片中确实存在多个不同单元格时才分别输出；不要把多个课程合并。JSON 使用紧凑单行格式。"""


def parse_json(text: str) -> tuple[dict | None, str | None]:
    candidate = re.sub(r"^\s*```(?:json)?\s*|\s*```\s*$", "", text.strip(), flags=re.I)
    try:
        return json.loads(candidate), None
    except json.JSONDecodeError as first:
        start, end = candidate.find("{"), candidate.rfind("}")
        if start >= 0 and end > start:
            try:
                return json.loads(candidate[start : end + 1]), None
            except json.JSONDecodeError as second:
                return None, str(second)
        return None, str(first)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("images", nargs="+")
    parser.add_argument("--model", default="artifacts/models/MiniCPM-V-4.6")
    parser.add_argument("--adapter", default="")
    parser.add_argument("--output", default="artifacts/ai-benchmark/minicpm-v-4.6-zero-shot.json")
    parser.add_argument("--downsample", default="16x", choices=("4x", "16x"))
    parser.add_argument("--max-slices", type=int, default=36)
    parser.add_argument("--max-pixels", type=int, default=1048576)
    parser.add_argument("--max-new-tokens", type=int, default=3072)
    args = parser.parse_args()

    model_path = str(Path(args.model).resolve())
    config = AutoConfig.from_pretrained(model_path, local_files_only=True)
    is_minicpm = config.model_type.startswith("minicpm")
    processor_kwargs = {} if is_minicpm else {"max_pixels": args.max_pixels}
    processor = AutoProcessor.from_pretrained(model_path, local_files_only=True, **processor_kwargs)
    model = AutoModelForImageTextToText.from_pretrained(
        model_path,
        dtype=torch.bfloat16,
        device_map="auto",
        local_files_only=True,
    ).eval()
    if args.adapter:
        from peft import PeftModel

        model = PeftModel.from_pretrained(model, str(Path(args.adapter).resolve()), local_files_only=True).eval()

    results = []
    for image_name in args.images:
        image_path = Path(image_name).resolve()
        with Image.open(image_path) as source:
            image = source.convert("RGB")

        messages = [{"role": "user", "content": [{"type": "image", "image": image}, {"type": "text", "text": PROMPT}]}]
        template_kwargs = {
            "tokenize": True,
            "add_generation_prompt": True,
            "return_dict": True,
            "return_tensors": "pt",
        }
        if is_minicpm:
            template_kwargs.update(downsample_mode=args.downsample, max_slice_nums=args.max_slices)
        inputs = processor.apply_chat_template(messages, **template_kwargs).to(model.device)

        if torch.cuda.is_available():
            torch.cuda.reset_peak_memory_stats()
            torch.cuda.synchronize()
        started = time.perf_counter()
        with torch.inference_mode():
            generation_kwargs = {"max_new_tokens": args.max_new_tokens, "do_sample": False}
            if is_minicpm:
                generation_kwargs["downsample_mode"] = args.downsample
            generated = model.generate(**inputs, **generation_kwargs)
        if torch.cuda.is_available():
            torch.cuda.synchronize()
        elapsed = time.perf_counter() - started
        trimmed = [out[len(original) :] for original, out in zip(inputs.input_ids, generated)]
        raw = processor.batch_decode(trimmed, skip_special_tokens=True, clean_up_tokenization_spaces=False)[0]
        parsed, error = parse_json(raw)
        results.append(
            {
                "image": str(image_path),
                "adapter": str(Path(args.adapter).resolve()) if args.adapter else "",
                "elapsedSeconds": round(elapsed, 3),
                "peakCudaMiB": round(torch.cuda.max_memory_allocated() / 1024 / 1024, 1) if torch.cuda.is_available() else 0,
                "inputTokens": int(inputs.input_ids.shape[-1]),
                "outputTokens": int(trimmed[0].shape[-1]),
                "jsonValid": parsed is not None,
                "parseError": error,
                "parsed": parsed,
                "raw": raw,
            }
        )
        output_path = Path(args.output)
        output_path.parent.mkdir(parents=True, exist_ok=True)
        output_path.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({k: results[-1][k] for k in ("image", "elapsedSeconds", "peakCudaMiB", "inputTokens", "outputTokens", "jsonValid")}, ensure_ascii=False), flush=True)


if __name__ == "__main__":
    main()
