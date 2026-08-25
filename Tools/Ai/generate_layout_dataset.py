"""Convert timetable labels into OCR-like text-with-boxes training records."""

from __future__ import annotations

import argparse
import json
import random
from pathlib import Path


DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
DAY_LABELS = ["星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日"]
TIMES = [
    ("08:00", "08:45"), ("08:55", "09:40"), ("10:00", "10:45"), ("10:55", "11:40"),
    ("14:00", "14:45"), ("14:55", "15:40"), ("16:00", "16:45"), ("16:55", "17:40"),
    ("19:00", "19:45"), ("19:55", "20:40"), ("20:50", "21:35"), ("21:45", "22:30"),
]

PROMPT = """你是课程表 OCR 版面结构化器。输入是离线 OCR 得到的文本块，每行格式为 [x0,y0,x1,y1] 文本，坐标已归一化到 0..1000。根据星期表头、时间/节次轴和文本块二维位置恢复课程。不要猜测不存在的信息；同一视觉课程块只输出一条。startTime/endTime 必须是 HH:mm。若不是个人周课程表，输出 other 和空 courses。只输出合法紧凑 JSON，字段固定为 schemaVersion、documentType、courses；课程字段固定为 name、teacher、location、dayOfWeek、startPeriod、endPeriod、startTime、endTime、weeks。"""


def box(x: int, y: int, width: int, height: int, text: str) -> str:
    return f"[{max(0, x)},{max(0, y)},{min(1000, x + width)},{min(1000, y + height)}] {text}"


def serialize(answer: dict, rng: random.Random) -> str:
    blocks = [box(330, 18, 340, 28, rng.choice(["本科生课程表", "我的课表", "课程安排"]))]
    if answer.get("documentType") != "weekly_schedule":
        for index in range(rng.randint(5, 10)):
            blocks.append(box(120, 100 + index * 70, 760, 35, f"{index + 1}. 课程清单项目 教师 教室"))
        return PROMPT + "\nOCR_BLOCKS:\n" + "\n".join(blocks)

    for day_index, label in enumerate(DAY_LABELS):
        x = 165 + day_index * 115 + rng.randint(-3, 3)
        blocks.append(box(x, 72 + rng.randint(-2, 2), 88, 28, label))
    for period, (start, end) in enumerate(TIMES, start=1):
        y = 125 + (period - 1) * 67 + rng.randint(-2, 2)
        blocks.append(box(18, y, 125, 25, rng.choice([f"第{period}节", f"{start}-{end}"])))

    for course in answer.get("courses", []):
        day = DAYS.index(course["dayOfWeek"])
        start = int(course["startPeriod"])
        end = int(course["endPeriod"])
        x = 155 + day * 115 + rng.randint(-5, 5)
        y0 = 116 + (start - 1) * 67 + rng.randint(-3, 3)
        y1 = 116 + end * 67 + rng.randint(-3, 3)
        width = 105
        fields = [course["name"], course["teacher"], course["location"], course["weeks"]]
        if rng.random() < 0.45:
            fields = [f"{fields[0]} {fields[1]}", f"{fields[2]} {fields[3]}"]
        step = max(18, (y1 - y0 - 12) // max(1, len(fields)))
        for index, text in enumerate(fields):
            blocks.append(box(x, y0 + 6 + index * step, width, min(28, step), text))

    # OCR engines usually return reading order, but small ordering perturbations are common.
    blocks.sort(key=lambda line: (int(line.split(",")[1]), int(line[1:].split(",")[0])))
    for index in range(1, len(blocks) - 1):
        if rng.random() < 0.04:
            blocks[index], blocks[index + 1] = blocks[index + 1], blocks[index]
    return PROMPT + "\nOCR_BLOCKS:\n" + "\n".join(blocks)


def convert(source: Path, target: Path, seed: int) -> int:
    rng = random.Random(seed)
    rows = []
    for index, line in enumerate(source.read_text(encoding="utf-8").splitlines()):
        original = json.loads(line)
        answer_text = original["messages"][-1]["content"]
        answer = json.loads(answer_text)
        rows.append({"id": f"{target.stem}_{index:05d}", "messages": [
            {"role": "user", "content": serialize(answer, rng)},
            {"role": "assistant", "content": answer_text},
        ]})
    target.parent.mkdir(parents=True, exist_ok=True)
    target.write_text("\n".join(json.dumps(row, ensure_ascii=False) for row in rows) + "\n", encoding="utf-8")
    return len(rows)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--source-dir", type=Path, default=Path("artifacts/ai-data/synthetic-v1"))
    parser.add_argument("--output-dir", type=Path, default=Path("artifacts/ai-data/layout-v1"))
    parser.add_argument("--seed", type=int, default=20260826)
    args = parser.parse_args()

    counts = {}
    for offset, split in enumerate(("train", "validation")):
        counts[split] = convert(args.source_dir / f"{split}.jsonl", args.output_dir / f"{split}.jsonl", args.seed + offset)
    info = {
        "schedule_layout_train": {
            "file_name": "train.jsonl", "formatting": "sharegpt", "columns": {"messages": "messages"},
            "tags": {"role_tag": "role", "content_tag": "content", "user_tag": "user", "assistant_tag": "assistant"},
        },
        "schedule_layout_validation": {
            "file_name": "validation.jsonl", "formatting": "sharegpt", "columns": {"messages": "messages"},
            "tags": {"role_tag": "role", "content_tag": "content", "user_tag": "user", "assistant_tag": "assistant"},
        },
    }
    (args.output_dir / "dataset_info.json").write_text(json.dumps(info, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    print(json.dumps(counts))


if __name__ == "__main__":
    main()
