"""Create a bounded JSONL subset for memory-safe high-resolution VLM training."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("input", type=Path)
    parser.add_argument("output", type=Path)
    parser.add_argument("--max-courses", type=int, default=12)
    parser.add_argument("--dataset-info", type=Path)
    parser.add_argument("--dataset-name", default="schedule_synthetic_train_short")
    args = parser.parse_args()

    kept: list[str] = []
    rejected = 0
    for line in args.input.read_text(encoding="utf-8").splitlines():
        row = json.loads(line)
        answer = json.loads(row["messages"][-1]["content"])
        if len(answer.get("courses", [])) <= args.max_courses:
            kept.append(json.dumps(row, ensure_ascii=False, separators=(",", ":")))
        else:
            rejected += 1

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text("\n".join(kept) + "\n", encoding="utf-8")

    if args.dataset_info:
        info = json.loads(args.dataset_info.read_text(encoding="utf-8"))
        info[args.dataset_name] = {
            "file_name": args.output.name,
            "formatting": "sharegpt",
            "columns": {"messages": "messages", "images": "images"},
            "tags": {"role_tag": "role", "content_tag": "content", "user_tag": "user", "assistant_tag": "assistant"},
        }
        args.dataset_info.write_text(json.dumps(info, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")

    print(json.dumps({"kept": len(kept), "rejected": rejected, "maxCourses": args.max_courses}))


if __name__ == "__main__":
    main()
