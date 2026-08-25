#!/usr/bin/env python3
"""Score structural validity and count recall for VLM timetable benchmarks."""

from __future__ import annotations

import argparse
import json
from pathlib import Path


REQUIRED_COURSE_FIELDS = {
    "name",
    "teacher",
    "location",
    "dayOfWeek",
    "startPeriod",
    "endPeriod",
    "startTime",
    "endTime",
    "weeks",
}
VALID_DAYS = {"Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"}


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("benchmark")
    parser.add_argument("--manifest", default="Tests/Fixtures/PublicSchedules/benchmark_manifest.json")
    args = parser.parse_args()

    results = json.loads(Path(args.benchmark).read_text(encoding="utf-8"))
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8"))
    expected = {sample["image"]: sample for sample in manifest["samples"]}
    rows = []
    for result in results:
        name = Path(result["image"]).name
        target = expected[name]
        payload = result.get("parsed")
        courses = payload.get("courses", []) if isinstance(payload, dict) else []
        schema_valid = isinstance(payload, dict) and payload.get("documentType") in {"weekly_schedule", "other"}
        if schema_valid:
            schema_valid = isinstance(courses, list) and all(
                isinstance(course, dict)
                and REQUIRED_COURSE_FIELDS.issubset(course)
                and course.get("dayOfWeek") in VALID_DAYS
                for course in courses
            )
        count_valid = target["expectedCourseCountMin"] <= len(courses) <= target["expectedCourseCountMax"]
        type_valid = isinstance(payload, dict) and payload.get("documentType") == target["documentType"]
        rows.append(
            {
                "image": name,
                "jsonValid": bool(result.get("jsonValid")),
                "schemaValid": schema_valid,
                "typeValid": type_valid,
                "courseCount": len(courses),
                "expectedCount": f"{target['expectedCourseCountMin']}-{target['expectedCourseCountMax']}",
                "countValid": count_valid,
                "elapsedSeconds": result.get("elapsedSeconds"),
                "peakCudaMiB": result.get("peakCudaMiB"),
            }
        )

    summary = {
        "samples": len(rows),
        "jsonValidRate": sum(x["jsonValid"] for x in rows) / len(rows),
        "schemaValidRate": sum(x["schemaValid"] for x in rows) / len(rows),
        "typeAccuracy": sum(x["typeValid"] for x in rows) / len(rows),
        "countRangeAccuracy": sum(x["countValid"] for x in rows) / len(rows),
        "averageSeconds": sum(x["elapsedSeconds"] for x in rows) / len(rows),
        "peakCudaMiB": max(x["peakCudaMiB"] for x in rows),
    }
    print(json.dumps({"summary": summary, "rows": rows}, ensure_ascii=False, indent=2))


if __name__ == "__main__":
    main()
