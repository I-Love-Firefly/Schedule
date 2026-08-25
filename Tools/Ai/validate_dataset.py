#!/usr/bin/env python3
"""Validate generated multimodal training data before expensive fine-tuning."""

from __future__ import annotations

import argparse
import json
from collections import Counter
from pathlib import Path

from jsonschema import Draft202012Validator
from PIL import Image


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("dataset")
    parser.add_argument("--schema", default="Tools/Ai/schedule_output.schema.json")
    args = parser.parse_args()

    root = Path(args.dataset)
    schema = json.loads(Path(args.schema).read_text(encoding="utf-8"))
    validator = Draft202012Validator(schema)
    stats = Counter()
    errors: list[str] = []
    for split in ("train", "validation"):
        records = [json.loads(line) for line in (root / f"{split}.jsonl").read_text(encoding="utf-8").splitlines() if line.strip()]
        for index, record in enumerate(records):
            stats[f"{split}.records"] += 1
            try:
                answer = json.loads(record["messages"][1]["content"])
            except (KeyError, IndexError, json.JSONDecodeError) as exc:
                errors.append(f"{split}:{index}: invalid conversation/JSON: {exc}")
                continue
            schema_errors = list(validator.iter_errors(answer))
            if schema_errors:
                errors.append(f"{split}:{index}: schema: {schema_errors[0].message}")
            image_path = Path(record["images"][0])
            try:
                with Image.open(image_path) as image:
                    image.verify()
            except Exception as exc:
                errors.append(f"{split}:{index}: image: {exc}")
            courses = answer["courses"]
            stats[f"{split}.courses"] += len(courses)
            stats[f"{split}.{answer['documentType']}"] += 1
            for course in courses:
                if course["startPeriod"] > course["endPeriod"]:
                    errors.append(f"{split}:{index}: reversed periods")
                if course["startTime"] >= course["endTime"]:
                    errors.append(f"{split}:{index}: reversed/equal times")

    report = {"valid": not errors, "stats": dict(stats), "errors": errors[:100]}
    print(json.dumps(report, ensure_ascii=False, indent=2))
    if errors:
        raise SystemExit(1)


if __name__ == "__main__":
    main()
