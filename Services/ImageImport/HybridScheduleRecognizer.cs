using System.Globalization;
using System.Text.RegularExpressions;

namespace Schedule2._0.Services.ImageImport;

public sealed class HybridScheduleRecognizer(ScheduleImageParser geometryParser, IScheduleAiService aiService)
{
    private static readonly HashSet<string> Days =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public async Task<ScheduleImageParseResult> RecognizeAsync(
        OcrDocument document,
        CancellationToken cancellationToken = default)
    {
        var geometry = geometryParser.Parse(document);
        if (!aiService.IsSupported || !aiService.IsModelInstalled)
        {
            geometry.Warnings.Add("未安装课程表 AI 模型，本次使用离线几何解析器。 ");
            return geometry;
        }

        AiScheduleDocument ai;
        try
        {
            ai = AiScheduleJsonParser.Parse(await aiService.StructureAsync(document, cancellationToken));
        }
        catch (Exception ex)
        {
            geometry.Warnings.Add($"本地 AI 输出未通过校验，已回退到几何解析器：{ex.Message}");
            return geometry;
        }

        if (!string.Equals(ai.DocumentType, "weekly_schedule", StringComparison.Ordinal))
        {
            if (geometry.Courses.Count > 0)
            {
                geometry.Warnings.Add("本地 AI 未确认这是周课程表，结果仍由几何解析器生成。 ");
                return geometry;
            }
            return new ScheduleImageParseResult { Warnings = { "图片不像个人周课程表，未生成可写入数据。" } };
        }

        var result = new ScheduleImageParseResult();
        var source = Normalize(string.Join(" ", document.Regions.Select(x => x.Text)));
        foreach (var item in ai.Courses)
        {
            if (!TryValidate(item, source, out var recognized, out var warning))
            {
                if (warning.Length > 0) result.Warnings.Add(warning);
                continue;
            }
            var geometryMatch = FindGeometryMatch(recognized, geometry.Courses);
            if (geometryMatch is not null)
            {
                recognized.DayOfWeek = geometryMatch.DayOfWeek;
                recognized.StartTime = geometryMatch.StartTime;
                recognized.EndTime = geometryMatch.EndTime;
                recognized.Confidence = Math.Max(recognized.Confidence, geometryMatch.Confidence);
            }
            result.Courses.Add(recognized);
        }

        RemoveDuplicates(result.Courses);
        var corroborated = CountCorroborated(result.Courses, geometry.Courses);
        var required = geometry.Courses.Count == 0 ? 0 : Math.Max(1, Math.Min(result.Courses.Count, geometry.Courses.Count) / 2);

        // Geometry remains the authority for rows/columns. AI may enrich fields,
        // but can never turn an unsafe geometry result into a database write.
        if (geometry.Courses.Count > 0)
        {
            foreach (var target in geometry.Courses)
            {
                var enhancement = FindAiMatch(target, result.Courses);
                if (enhancement is null) continue;
                if (!string.IsNullOrWhiteSpace(enhancement.Teacher) && source.Contains(Normalize(enhancement.Teacher), StringComparison.OrdinalIgnoreCase))
                    target.Teacher = enhancement.Teacher;
                if (!string.IsNullOrWhiteSpace(enhancement.Location) && source.Contains(Normalize(enhancement.Location), StringComparison.OrdinalIgnoreCase))
                    target.Location = enhancement.Location;
                target.SourceText = $"{target.SourceText} | AI 已复核字段";
            }
            if (corroborated == 0)
                geometry.Warnings.Add("本地 AI 未能可靠复核本次版式，结果仍由几何解析器生成。 ");
            return geometry;
        }

        result.IsWriteSafe = false;

        if (corroborated < required)
            result.Warnings.Add("AI 缺少可由几何坐标复核的课程，已禁止直接写入，请人工校对。 ");
        if (result.Courses.Count == 0)
            result.Warnings.Add("本地 AI 没有生成通过字段校验的课程。 ");
        return result;
    }

    private static bool TryValidate(AiScheduleCourse item, string source, out RecognizedCourse course, out string warning)
    {
        course = new RecognizedCourse();
        warning = "";
        var name = item.Name.Trim();
        if (name.Length is < 2 or > 80 || !Days.Contains(item.DayOfWeek) ||
            !TryTime(item.StartTime, out var start) || !TryTime(item.EndTime, out var end) || start >= end)
        {
            warning = $"课程“{name}”的名称、星期或时间无效，已忽略。";
            return false;
        }

        if (!source.Contains(Normalize(name), StringComparison.OrdinalIgnoreCase))
        {
            warning = $"课程“{name}”无法在 OCR 原文中找到，可能是模型幻觉，已忽略。";
            return false;
        }

        course = new RecognizedCourse
        {
            Name = name,
            Teacher = item.Teacher.Trim(),
            Location = item.Location.Trim(),
            DayOfWeek = item.DayOfWeek,
            StartTime = start.ToString("hh.mmtt", CultureInfo.InvariantCulture).ToLowerInvariant(),
            EndTime = end.ToString("hh.mmtt", CultureInfo.InvariantCulture).ToLowerInvariant(),
            Confidence = 0.82,
            SourceText = $"AI+OCR：{name} {item.Teacher} {item.Location}".Trim()
        };
        return true;
    }

    private static bool TryTime(string value, out DateTime time) =>
        DateTime.TryParseExact(value.Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out time);

    private static int CountCorroborated(List<RecognizedCourse> ai, List<RecognizedCourse> geometry) =>
        ai.Count(candidate => geometry.Any(other =>
            string.Equals(candidate.DayOfWeek, other.DayOfWeek, StringComparison.OrdinalIgnoreCase) &&
            (Normalize(candidate.Name).Contains(Normalize(other.Name), StringComparison.OrdinalIgnoreCase) ||
             Normalize(other.Name).Contains(Normalize(candidate.Name), StringComparison.OrdinalIgnoreCase) ||
             candidate.StartTime == other.StartTime)));

    private static RecognizedCourse? FindGeometryMatch(RecognizedCourse candidate, List<RecognizedCourse> geometry) =>
        geometry
            .Where(other => string.Equals(candidate.DayOfWeek, other.DayOfWeek, StringComparison.OrdinalIgnoreCase))
            .Select(other => new
            {
                Course = other,
                Score = Normalize(candidate.Name).Contains(Normalize(other.Name), StringComparison.OrdinalIgnoreCase) ||
                        Normalize(other.Name).Contains(Normalize(candidate.Name), StringComparison.OrdinalIgnoreCase) ? 2 :
                        candidate.StartTime == other.StartTime ? 1 : 0
            })
            .Where(x => x.Score > 0)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Course)
            .FirstOrDefault();

    private static RecognizedCourse? FindAiMatch(RecognizedCourse geometry, List<RecognizedCourse> ai) =>
        ai.FirstOrDefault(candidate =>
            string.Equals(candidate.DayOfWeek, geometry.DayOfWeek, StringComparison.OrdinalIgnoreCase) &&
            (Normalize(candidate.Name).Contains(Normalize(geometry.Name), StringComparison.OrdinalIgnoreCase) ||
             Normalize(geometry.Name).Contains(Normalize(candidate.Name), StringComparison.OrdinalIgnoreCase)));

    private static void RemoveDuplicates(List<RecognizedCourse> courses)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        courses.RemoveAll(x => !seen.Add($"{x.DayOfWeek}|{x.StartTime}|{Normalize(x.Name)}"));
    }

    private static string Normalize(string value) => Regex.Replace(value, @"\s+", "").Trim();
}
