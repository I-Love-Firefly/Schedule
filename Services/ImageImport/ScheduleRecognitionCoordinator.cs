using System.Globalization;
using System.Text;

namespace Schedule2._0.Services.ImageImport;

public sealed class ScheduleRecognitionCoordinator(
    ScheduleImageParser geometryParser,
    ICloudScheduleAiService cloudService)
{
    private static readonly HashSet<string> Days =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    public async Task<ScheduleImageParseResult> RecognizeAsync(
        string imagePath,
        OcrDocument document,
        CancellationToken cancellationToken = default)
    {
        if (!cloudService.IsEnabled)
            throw new InvalidOperationException("课程表 API 地址无效，无法识别图片。");

        var response = await cloudService.RecognizeAsync(imagePath, document, cancellationToken);
        var result = BuildCloudResult(document, response);
        result.RecognitionSource = $"DeepSeek API（{response.Model}，{response.ElapsedMs / 1000d:F1} 秒）";
        return result;
    }

    private ScheduleImageParseResult BuildCloudResult(OcrDocument document, CloudScheduleAiResponse response)
    {
        var parsed = AiScheduleJsonParser.Parse(response.Content);
        if (!string.Equals(parsed.DocumentType, "weekly_schedule", StringComparison.Ordinal))
            return new ScheduleImageParseResult
            {
                RecognitionSource = "云端大模型",
                Warnings = { "云端模型判断这不是个人周课程表，未生成课程。" }
            };

        var geometry = geometryParser.Parse(document);
        var result = new ScheduleImageParseResult();
        var evidence = NormalizeEvidence(string.Join(' ', document.Regions.Select(x => x.Text)));
        var rejected = 0;
        var corroborated = 0;

        foreach (var item in parsed.Courses)
        {
            if (!TryConvert(item, evidence, out var course))
            {
                rejected++;
                continue;
            }

            var match = FindGeometryMatch(course, geometry.Courses);
            if (match is not null)
            {
                course.DayOfWeek = match.DayOfWeek;
                course.StartTime = match.StartTime;
                course.EndTime = match.EndTime;
                course.Confidence = 0.94;
                corroborated++;
            }
            result.Courses.Add(course);
        }

        RemoveDuplicates(result.Courses);
        var enoughGeometry = geometry.Courses.Count > 0 &&
                             corroborated >= Math.Max(1, Math.Min(result.Courses.Count, geometry.Courses.Count) / 2);
        var allTimesVisible = result.Courses.Count > 0 && result.Courses.All(x =>
            ContainsTimeEvidence(evidence, x.StartTime) && ContainsTimeEvidence(evidence, x.EndTime));

        result.IsWriteSafe = result.Courses.Count > 0 && rejected == 0 && (enoughGeometry || allTimesVisible);
        if (rejected > 0)
            result.Warnings.Add($"云端模型有 {rejected} 条结果无法由本地 OCR 证据确认，已忽略。 ");
        if (!enoughGeometry)
            result.Warnings.Add("部分课程的星期或时间未被本地网格解析器复核，请重点检查。 ");
        if (!result.IsWriteSafe && result.Courses.Count > 0)
            result.Warnings.Add("结果需要人工逐项确认后才能写入数据库。 ");
        if (result.Courses.Count == 0)
            result.Warnings.Add("云端模型没有生成可由 OCR 原文确认的课程。 ");
        return result;
    }

    private static bool TryConvert(AiScheduleCourse item, string evidence, out RecognizedCourse course)
    {
        course = new RecognizedCourse();
        var name = item.Name.Trim();
        if (name.Length is < 2 or > 80 || !Days.Contains(item.DayOfWeek) ||
            !TryTime(item.StartTime, out var start) || !TryTime(item.EndTime, out var end) || start >= end)
            return false;

        var normalizedName = NormalizeEvidence(name);
        if (normalizedName.Length < 2 || !evidence.Contains(normalizedName, StringComparison.OrdinalIgnoreCase))
            return false;

        course = new RecognizedCourse
        {
            Name = name,
            Teacher = item.Teacher.Trim(),
            Location = item.Location.Trim(),
            DayOfWeek = item.DayOfWeek,
            StartTime = start.ToString("hh.mmtt", CultureInfo.InvariantCulture).ToLowerInvariant(),
            EndTime = end.ToString("hh.mmtt", CultureInfo.InvariantCulture).ToLowerInvariant(),
            Confidence = 0.84,
            SourceText = $"DeepSeek API + 设备 OCR 校验：{name} {item.Teacher} {item.Location}".Trim()
        };
        return true;
    }

    private static RecognizedCourse? FindGeometryMatch(RecognizedCourse candidate, List<RecognizedCourse> geometry) =>
        geometry.FirstOrDefault(other =>
            string.Equals(candidate.DayOfWeek, other.DayOfWeek, StringComparison.OrdinalIgnoreCase) &&
            (NormalizeEvidence(candidate.Name).Contains(NormalizeEvidence(other.Name), StringComparison.OrdinalIgnoreCase) ||
             NormalizeEvidence(other.Name).Contains(NormalizeEvidence(candidate.Name), StringComparison.OrdinalIgnoreCase) ||
             candidate.StartTime == other.StartTime));

    private static bool TryTime(string value, out DateTime time) =>
        DateTime.TryParseExact(value.Trim(), ["H:mm", "HH:mm"], CultureInfo.InvariantCulture,
            DateTimeStyles.None, out time);

    private static string NormalizeEvidence(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var ch in value.Normalize(NormalizationForm.FormKC).ToLowerInvariant())
            if (char.IsLetterOrDigit(ch)) builder.Append(ch);
        return builder.ToString();
    }

    private static bool ContainsTimeEvidence(string evidence, string displayTime)
    {
        if (!DateTime.TryParseExact(displayTime, "hh.mmtt", CultureInfo.InvariantCulture,
                DateTimeStyles.None, out var time)) return false;
        var hour = time.Hour;
        var minute = time.Minute;
        return evidence.Contains($"{hour}{minute:00}", StringComparison.Ordinal) ||
               evidence.Contains($"{hour:00}{minute:00}", StringComparison.Ordinal);
    }

    private static void RemoveDuplicates(List<RecognizedCourse> courses)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        courses.RemoveAll(course => !seen.Add(
            $"{NormalizeEvidence(course.Name)}|{course.DayOfWeek}|{course.StartTime}|{course.EndTime}"));
    }
}
