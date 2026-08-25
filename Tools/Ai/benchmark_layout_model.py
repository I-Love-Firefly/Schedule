#!/usr/bin/env python3
"""Benchmark a text model on normalized OCR text blocks."""

from __future__ import annotations

import argparse
import json
import re
import time
from pathlib import Path

import torch
from transformers import AutoModelForCausalLM, AutoTokenizer


def parse_json(text: str) -> tuple[dict | None, str | None]:
    candidate = re.sub(r"<think>[\s\S]*?</think>\s*", "", text.strip()).strip()
    candidate = re.sub(r"^\s*```(?:json)?\s*|\s*```\s*$", "", candidate, flags=re.I)
    try:
        return json.loads(candidate), None
    except json.JSONDecodeError as first:
        start, end = candidate.find("{"), candidate.rfind("}")
        if start >= 0 and end > start:
            try:
                return json.loads(candidate[start : end + 1]), None
            except json.JSONDecodeError:
                pass
        recovered = recover_complete_courses(candidate)
        return (recovered, "recovered_truncated_json") if recovered else (None, str(first))


def recover_complete_courses(candidate: str) -> dict | None:
    if '"documentType":"weekly_schedule"' not in candidate.replace(" ", ""):
        return None
    marker = candidate.find('"courses"')
    array_start = candidate.find("[", marker) if marker >= 0 else -1
    if array_start < 0:
        return None
    courses, depth, start, in_string, escaped = [], 0, -1, False, False
    for index, char in enumerate(candidate[array_start + 1 :], start=array_start + 1):
        if in_string:
            if escaped:
                escaped = False
            elif char == "\\":
                escaped = True
            elif char == '"':
                in_string = False
            continue
        if char == '"':
            in_string = True
        elif char == "{":
            if depth == 0:
                start = index
            depth += 1
        elif char == "}" and depth:
            depth -= 1
            if depth == 0 and start >= 0:
                try:
                    courses.append(json.loads(candidate[start : index + 1]))
                except json.JSONDecodeError:
                    pass
                start = -1
    return {"schemaVersion": 1, "documentType": "weekly_schedule", "courses": courses} if courses else None


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("dataset", type=Path)
    parser.add_argument("--model", default="artifacts/models/MiniCPM5-1B")
    parser.add_argument("--adapter", default="")
    parser.add_argument("--output", required=True, type=Path)
    parser.add_argument("--limit", type=int, default=10)
    parser.add_argument("--offset", type=int, default=0)
    parser.add_argument("--max-new-tokens", type=int, default=3072)
    args = parser.parse_args()

    tokenizer = AutoTokenizer.from_pretrained(args.model, local_files_only=True)
    model = AutoModelForCausalLM.from_pretrained(
        args.model, dtype=torch.bfloat16, device_map="auto", local_files_only=True
    ).eval()
    if args.adapter:
        from peft import PeftModel
        model = PeftModel.from_pretrained(model, str(Path(args.adapter).resolve()), local_files_only=True).eval()

    lines = args.dataset.read_text(encoding="utf-8").splitlines()
    rows = [json.loads(line) for line in lines[args.offset : args.offset + args.limit]]
    results = []
    for row in rows:
        messages = [{"role": "user", "content": row["messages"][0]["content"]}]
        input_ids = tokenizer.apply_chat_template(
            messages, add_generation_prompt=True, enable_thinking=False, return_tensors="pt"
        )
        # Transformers 5 may return either a tensor or a BatchEncoding here.
        # generate() expects the tensor when passed positionally.
        if hasattr(input_ids, "input_ids"):
            input_ids = input_ids.input_ids
        input_ids = input_ids.to(model.device)
        if torch.cuda.is_available():
            torch.cuda.reset_peak_memory_stats()
            torch.cuda.synchronize()
        started = time.perf_counter()
        with torch.inference_mode():
            generated = model.generate(
                input_ids,
                attention_mask=torch.ones_like(input_ids),
                max_new_tokens=args.max_new_tokens,
                do_sample=False,
            )
        if torch.cuda.is_available():
            torch.cuda.synchronize()
        raw = tokenizer.decode(generated[0, input_ids.shape[-1] :], skip_special_tokens=True)
        parsed, error = parse_json(raw)
        result = {
            "id": row["id"], "image": row["id"], "elapsedSeconds": round(time.perf_counter() - started, 3),
            "peakCudaMiB": round(torch.cuda.max_memory_allocated() / 1024 / 1024, 1) if torch.cuda.is_available() else 0,
            "inputTokens": int(input_ids.shape[-1]), "outputTokens": int(generated.shape[-1] - input_ids.shape[-1]),
            "jsonValid": parsed is not None and error is None,
            "recovered": error == "recovered_truncated_json",
            "parseError": error, "parsed": parsed, "raw": raw,
        }
        results.append(result)
        args.output.parent.mkdir(parents=True, exist_ok=True)
        args.output.write_text(json.dumps(results, ensure_ascii=False, indent=2), encoding="utf-8")
        print(json.dumps({key: result[key] for key in ("id", "elapsedSeconds", "inputTokens", "outputTokens", "jsonValid")}), flush=True)


if __name__ == "__main__":
    main()
