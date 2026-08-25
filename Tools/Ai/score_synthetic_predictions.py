#!/usr/bin/env python3
"""Compute exact structured metrics against synthetic timetable labels."""

from __future__ import annotations

import argparse
import json
from pathlib import Path

from jsonschema import Draft202012Validator


FIELDS = ("name", "teacher", "location", "dayOfWeek", "startPeriod", "endPeriod", "startTime", "endTime", "weeks")
CORE = ("name", "dayOfWeek", "startPeriod", "endPeriod")


def key(course: dict, fields: tuple[str, ...]) -> tuple:
    return tuple(course.get(field) for field in fields)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("predictions")
    parser.add_argument("ground_truth_jsonl")
    parser.add_argument("--schema", default="Tools/Ai/schedule_output.schema.json")
    args = parser.parse_args()

    predictions = json.loads(Path(args.predictions).read_text(encoding="utf-8"))
    truth = {}
    for line in Path(args.ground_truth_jsonl).read_text(encoding="utf-8").splitlines():
        row = json.loads(line)
        identity = row.get("id") or Path(row["images"][0]).name
        truth[identity] = json.loads(row["messages"][1]["content"])
    validator = Draft202012Validator(json.loads(Path(args.schema).read_text(encoding="utf-8")))

    totals = {"samples": 0, "json": 0, "schema": 0, "type": 0, "exactDocuments": 0, "tp": 0, "pred": 0, "gold": 0}
    field_hits = {field: 0 for field in FIELDS}
    matched = 0
    rows = []
    for result in predictions:
        name = result.get("id") or Path(result["image"]).name
        if name not in truth:
            continue
        gold = truth[name]
        pred = result.get("parsed")
        totals["samples"] += 1
        totals["json"] += int(pred is not None)
        schema_valid = isinstance(pred, dict) and not list(validator.iter_errors(pred))
        totals["schema"] += int(schema_valid)
        pred_courses = pred.get("courses", []) if isinstance(pred, dict) else []
        gold_courses = gold["courses"]
        totals["type"] += int(isinstance(pred, dict) and pred.get("documentType") == gold["documentType"])
        totals["pred"] += len(pred_courses)
        totals["gold"] += len(gold_courses)
        pred_core = {key(course, CORE): course for course in pred_courses}
        gold_core = {key(course, CORE): course for course in gold_courses}
        common = pred_core.keys() & gold_core.keys()
        totals["tp"] += len(common)
        matched += len(common)
        for course_key in common:
            for field in FIELDS:
                field_hits[field] += int(pred_core[course_key].get(field) == gold_core[course_key].get(field))
        exact = isinstance(pred, dict) and pred == gold
        totals["exactDocuments"] += int(exact)
        rows.append({"image": name, "jsonValid": pred is not None, "schemaValid": schema_valid, "predicted": len(pred_courses), "expected": len(gold_courses), "matchedCore": len(common), "exact": exact})

    samples = max(totals["samples"], 1)
    precision = totals["tp"] / max(totals["pred"], 1)
    recall = totals["tp"] / max(totals["gold"], 1)
    report = {
        "summary": {
            "samples": totals["samples"],
            "jsonValidRate": totals["json"] / samples,
            "schemaValidRate": totals["schema"] / samples,
            "documentTypeAccuracy": totals["type"] / samples,
            "exactDocumentRate": totals["exactDocuments"] / samples,
            "coursePrecision": precision,
            "courseRecall": recall,
            "courseF1": 2 * precision * recall / max(precision + recall, 1e-12),
            "fieldAccuracyOnMatchedCourses": {field: field_hits[field] / max(matched, 1) for field in FIELDS},
        },
        "rows": rows,
    }
    print(json.dumps(report, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
