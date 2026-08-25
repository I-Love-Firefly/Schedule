#!/usr/bin/env python3
"""Generate diverse timetable images with exact structured labels."""

from __future__ import annotations

import argparse
import json
import random
from pathlib import Path

from PIL import Image, ImageDraw, ImageFilter, ImageFont


DAYS = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"]
DAY_LABELS = {
    "full": ["星期一", "星期二", "星期三", "星期四", "星期五", "星期六", "星期日"],
    "short": ["周一", "周二", "周三", "周四", "周五", "周六", "周日"],
    "single": ["一", "二", "三", "四", "五", "六", "日"],
    "english": DAYS,
}
TIMES = [
    ("08:00", "08:45"), ("08:55", "09:40"), ("10:00", "10:45"), ("10:55", "11:40"),
    ("14:00", "14:45"), ("14:55", "15:40"), ("16:00", "16:45"), ("16:55", "17:40"),
    ("19:00", "19:45"), ("19:55", "20:40"), ("20:50", "21:35"), ("21:45", "22:30"),
]
COURSES = [
    "高等数学", "大学英语", "数据结构", "计算机组成原理", "操作系统", "线性代数", "概率论与数理统计",
    "思想道德与法治", "中国近现代史纲要", "大学物理", "数字电路", "模拟电子技术", "信号与系统",
    "软件工程", "数据库原理", "人工智能导论", "机器学习", "计算机网络", "离散数学", "编译原理",
    "教育研究方法", "现代文学", "社会心理学", "项目管理", "通信电子线路", "微机系统与接口实验",
]
TEACHERS = ["张伟", "李娜", "王强", "刘洋", "陈晨", "杨帆", "赵磊", "黄文锋", "左小德", "周老师", "Joshua A. Dunlop"]
LOCATIONS = ["博学楼207", "教学楼A101", "3J-419", "奉贤3教113", "雷禺楼425", "品学楼B208", "基础实验楼乙109", "线上教学", "A4#G01"]
WEEKS = ["1-16周", "1-8周", "9-16周", "1-14周", "单周", "双周", "3-12周"]

PROMPT = """你是课程表结构化识别器。读取整张图片，只提取学生实际需要上课的课程。忽略标题、节次轴、星期表头、备注、课程清单和空白单元格。不要猜测图片中不存在的信息。一个可见课程单元格只能输出一条记录，周次保持为一个字符串，不得按每周展开。若不是个人周课程表，documentType 输出 other 且 courses 为空。只输出合法紧凑 JSON。"""


def font_path() -> str:
    candidates = [
        Path("C:/Windows/Fonts/msyh.ttc"), Path("C:/Windows/Fonts/simhei.ttf"),
        Path("C:/Windows/Fonts/simsun.ttc"),
    ]
    for path in candidates:
        if path.exists():
            return str(path)
    raise FileNotFoundError("No Chinese font found")


FONT_PATH = font_path()


def font(size: int) -> ImageFont.FreeTypeFont:
    return ImageFont.truetype(FONT_PATH, size=size)


def minutes(value: str) -> int:
    hour, minute = (int(part) for part in value.split(":"))
    return hour * 60 + minute


def text_size(draw: ImageDraw.ImageDraw, text: str, text_font: ImageFont.FreeTypeFont) -> tuple[int, int]:
    box = draw.textbbox((0, 0), text, font=text_font)
    return box[2] - box[0], box[3] - box[1]


def fit_lines(draw: ImageDraw.ImageDraw, parts: list[str], width: int, height: int, max_size: int) -> tuple[ImageFont.FreeTypeFont, list[str]]:
    for size in range(max_size, 11, -2):
        current = font(size)
        lines: list[str] = []
        for part in parts:
            if text_size(draw, part, current)[0] <= width:
                lines.append(part)
                continue
            line = ""
            for char in part:
                if text_size(draw, line + char, current)[0] <= width:
                    line += char
                else:
                    if line:
                        lines.append(line)
                    line = char
            if line:
                lines.append(line)
        line_height = size + 5
        if len(lines) * line_height <= height:
            return current, lines
    return font(12), parts[: max(1, height // 17)]


def render_grid(rng: random.Random, family: int, negative: bool) -> tuple[Image.Image, dict]:
    width, height = rng.choice([(1600, 1100), (1920, 1080), (1440, 1200)])
    dark = family == 8
    background = (28, 31, 36) if dark else (248, 249, 251)
    foreground = (238, 240, 244) if dark else (25, 30, 38)
    grid_color = (100, 105, 115) if dark else (95, 105, 118)
    image = Image.new("RGB", (width, height), background)
    draw = ImageDraw.Draw(image)

    title_h = rng.randint(60, 105)
    margin = rng.randint(24, 60)
    title = rng.choice(["本科生课程表", "我的课表", "2026-2027学年第一学期课程表", "课程安排"])
    title_font = font(rng.randint(26, 38))
    tw, _ = text_size(draw, title, title_font)
    draw.text(((width - tw) / 2, margin), title, font=title_font, fill=foreground)

    day_count = rng.choice([5, 5, 5, 6, 7])
    periods = rng.choice([8, 10, 12])
    table_top = margin + title_h
    table_bottom = height - margin
    time_width = rng.randint(125, 190)
    table_left, table_right = margin, width - margin
    header_h = rng.randint(48, 72)
    row_h = (table_bottom - table_top - header_h) / periods
    col_w = (table_right - table_left - time_width) / day_count
    labels_kind = {5: "english", 6: "single", 7: "full", 8: "short"}.get(family, rng.choice(list(DAY_LABELS)))
    labels = DAY_LABELS[labels_kind]
    day_order = list(range(day_count))
    if family == 7 and day_count == 7:
        day_order = [6, 0, 1, 2, 3, 4, 5]

    header_fill = (42, 82, 118) if dark else rng.choice([(91, 170, 215), (83, 195, 174), (126, 145, 210)])
    draw.rectangle((table_left, table_top, table_right, table_top + header_h), fill=header_fill)
    if family not in {2, 9}:
        for row in range(periods + 2):
            y = table_top if row == 0 else table_top + header_h + (row - 1) * row_h
            draw.line((table_left, y, table_right, y), fill=grid_color, width=2)
        for col in range(day_count + 2):
            x = table_left if col == 0 else table_left + time_width + (col - 1) * col_w
            draw.line((x, table_top, x, table_bottom), fill=grid_color, width=2)

    header_font = font(rng.randint(20, 27))
    draw.text((table_left + 20, table_top + 12), rng.choice(["节次", "时间", "Time"]), font=header_font, fill=foreground)
    for visual_index, day_index in enumerate(day_order):
        label = labels[day_index]
        x0 = table_left + time_width + visual_index * col_w
        label_w, label_h = text_size(draw, label, header_font)
        draw.text((x0 + (col_w - label_w) / 2, table_top + (header_h - label_h) / 2), label, font=header_font, fill=foreground)

    axis_font = font(rng.randint(15, 20))
    for row in range(periods):
        y0 = table_top + header_h + row * row_h
        if family in {1, 4, 6}:
            label = f"第{row + 1}节"
        else:
            label = f"{TIMES[row][0]}-{TIMES[row][1]}"
        lw, lh = text_size(draw, label, axis_font)
        draw.text((table_left + (time_width - lw) / 2, y0 + (row_h - lh) / 2), label, font=axis_font, fill=foreground)

    if negative:
        list_font = font(19)
        y = table_top + header_h + 12
        for index in range(min(periods, 9)):
            text = f"{index + 1:02d}  {rng.choice(COURSES)}  {rng.choice(TEACHERS)}  {rng.choice(LOCATIONS)}"
            draw.text((table_left + time_width + 18, y), text, font=list_font, fill=foreground)
            y += row_h
        return image, {"schemaVersion": 1, "documentType": "other", "courses": []}

    occupied = [[False] * periods for _ in range(day_count)]
    course_count = rng.randint(6, min(18, day_count * periods // 2))
    courses = []
    attempts = 0
    while len(courses) < course_count and attempts < course_count * 30:
        attempts += 1
        day_index = rng.randrange(day_count)
        span = rng.choice([1, 2, 2, 2, 3])
        valid_starts = [
            candidate for candidate in range(0, periods - span + 1)
            if all(minutes(TIMES[row + 1][0]) - minutes(TIMES[row][1]) <= 30 for row in range(candidate, candidate + span - 1))
        ]
        start = rng.choice(valid_starts)
        if any(occupied[day_index][start : start + span]):
            continue
        for row in range(start, start + span):
            occupied[day_index][row] = True
        visual_index = day_order.index(day_index)
        x0 = table_left + time_width + visual_index * col_w + 3
        x1 = x0 + col_w - 6
        y0 = table_top + header_h + start * row_h + 3
        y1 = table_top + header_h + (start + span) * row_h - 3
        name, teacher, location, weeks = rng.choice(COURSES), rng.choice(TEACHERS), rng.choice(LOCATIONS), rng.choice(WEEKS)
        fill = rng.choice([(180, 220, 247), (190, 235, 220), (245, 215, 170), (219, 204, 245), (247, 196, 205)])
        if dark:
            fill = tuple(max(45, channel // 3) for channel in fill)
        draw.rounded_rectangle((x0, y0, x1, y1), radius=rng.randint(3, 13), fill=fill, outline=grid_color, width=1)
        if family in {4, 9}:
            parts = [f"{name} {teacher}", f"{location} {weeks}"]
        elif family == 1:
            parts = [name, f"{teacher} / {location}", weeks]
        else:
            parts = [name, teacher, location, weeks]
        cell_font, lines = fit_lines(draw, parts, int(col_w - 14), int(y1 - y0 - 10), min(22, int(row_h * 0.34)))
        line_height = cell_font.size + 4
        text_y = y0 + max(5, (y1 - y0 - len(lines) * line_height) / 2)
        for line in lines:
            line_w, _ = text_size(draw, line, cell_font)
            draw.text((x0 + max(5, (x1 - x0 - line_w) / 2), text_y), line, font=cell_font, fill=foreground)
            text_y += line_height
        courses.append(
            {
                "name": name,
                "teacher": teacher,
                "location": location,
                "dayOfWeek": DAYS[day_index],
                "startPeriod": start + 1,
                "endPeriod": start + span,
                "startTime": TIMES[start][0],
                "endTime": TIMES[start + span - 1][1],
                "weeks": weeks,
            }
        )
    courses.sort(key=lambda x: (DAYS.index(x["dayOfWeek"]), x["startPeriod"], x["name"]))
    return image, {"schemaVersion": 1, "documentType": "weekly_schedule", "courses": courses}


def render_cards(rng: random.Random, negative: bool) -> tuple[Image.Image, dict]:
    width, height = 1080, 2200
    image = Image.new("RGB", (width, height), (244, 250, 248))
    draw = ImageDraw.Draw(image)
    draw.text((55, 55), "课程表助手", font=font(48), fill=(30, 40, 55))
    if negative:
        draw.text((55, 150), "本学期课程清单", font=font(31), fill=(60, 80, 90))
        for i in range(12):
            draw.text((75, 240 + i * 120), f"{i + 1}. {rng.choice(COURSES)}  {rng.choice(TEACHERS)}", font=font(24), fill=(35, 45, 55))
        return image, {"schemaVersion": 1, "documentType": "other", "courses": []}
    courses = []
    y = 150
    for day_index in range(rng.choice([5, 6, 7])):
        day_courses = rng.randint(1, 3)
        draw.text((55, y), DAY_LABELS[rng.choice(["full", "short", "english"])][day_index], font=font(30), fill=(85, 95, 115))
        y += 58
        for _ in range(day_courses):
            if y + 190 > height:
                break
            start = rng.choice([0, 2, 4, 6, 8])
            span = rng.choice([1, 2])
            name, teacher, location, weeks = rng.choice(COURSES), rng.choice(TEACHERS), rng.choice(LOCATIONS), rng.choice(WEEKS)
            draw.rounded_rectangle((65, y, width - 65, y + 170), radius=24, fill=(215, 240, 235))
            draw.text((95, y + 28), f"{TIMES[start][0]} - {TIMES[start + span - 1][1]}", font=font(23), fill=(30, 45, 55))
            draw.text((365, y + 25), name, font=font(28), fill=(25, 35, 50))
            draw.text((365, y + 77), f"{teacher}  {location}", font=font(22), fill=(50, 120, 112))
            draw.text((365, y + 119), weeks, font=font(19), fill=(90, 100, 110))
            courses.append({"name": name, "teacher": teacher, "location": location, "dayOfWeek": DAYS[day_index], "startPeriod": start + 1, "endPeriod": start + span, "startTime": TIMES[start][0], "endTime": TIMES[start + span - 1][1], "weeks": weeks})
            y += 192
    return image, {"schemaVersion": 1, "documentType": "weekly_schedule", "courses": courses}


def augment(rng: random.Random, image: Image.Image) -> Image.Image:
    if rng.random() < 0.25:
        image = image.filter(ImageFilter.GaussianBlur(radius=rng.uniform(0.15, 0.65)))
    if rng.random() < 0.25:
        angle = rng.uniform(-0.7, 0.7)
        image = image.rotate(angle, resample=Image.Resampling.BICUBIC, expand=False, fillcolor=image.getpixel((0, 0)))
    return image


def record(image_path: Path, answer: dict) -> dict:
    return {
        "messages": [
            {"role": "user", "content": f"<image>{PROMPT}"},
            {"role": "assistant", "content": json.dumps(answer, ensure_ascii=False, separators=(",", ":"))},
        ],
        "images": [str(image_path.resolve())],
    }


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--output", default="artifacts/ai-data/synthetic-v1")
    parser.add_argument("--train", type=int, default=600)
    parser.add_argument("--validation", type=int, default=100)
    parser.add_argument("--seed", type=int, default=20260826)
    parser.add_argument("--negative-ratio", type=float, default=0.12)
    args = parser.parse_args()

    output = Path(args.output)
    (output / "images").mkdir(parents=True, exist_ok=True)
    rng = random.Random(args.seed)
    split_records: dict[str, list[dict]] = {"train": [], "validation": []}
    metadata = {"seed": args.seed, "train": args.train, "validation": args.validation, "negativeRatio": args.negative_ratio, "families": {}}
    for split, count in (("train", args.train), ("validation", args.validation)):
        families = list(range(0, 8)) if split == "train" else [8, 9]
        metadata["families"][split] = families
        for index in range(count):
            family = rng.choice(families)
            negative = rng.random() < args.negative_ratio
            image, answer = render_cards(rng, negative) if family == 3 else render_grid(rng, family, negative)
            image = augment(rng, image)
            image_path = output / "images" / f"{split}_{index:05d}_f{family}.jpg"
            image.save(image_path, "JPEG", quality=rng.randint(78, 96), optimize=True)
            split_records[split].append(record(image_path, answer))
        target = output / f"{split}.jsonl"
        target.write_text("\n".join(json.dumps(row, ensure_ascii=False) for row in split_records[split]) + "\n", encoding="utf-8")
    (output / "metadata.json").write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
    dataset_info = {}
    for split in ("train", "validation"):
        dataset_info[f"schedule_synthetic_{split}"] = {
            "file_name": f"{split}.jsonl",
            "formatting": "sharegpt",
            "columns": {"messages": "messages", "images": "images"},
            "tags": {
                "role_tag": "role",
                "content_tag": "content",
                "user_tag": "user",
                "assistant_tag": "assistant",
            },
        }
    (output / "dataset_info.json").write_text(json.dumps(dataset_info, ensure_ascii=False, indent=2), encoding="utf-8")
    print(json.dumps(metadata, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
