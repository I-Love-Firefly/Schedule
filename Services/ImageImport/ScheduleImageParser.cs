using System.Globalization;
using System.Text.RegularExpressions;

namespace Schedule2._0.Services.ImageImport;

public sealed partial class ScheduleImageParser
{
    private static readonly string[] Days =
        ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];

    private static readonly string[][] DayAliases =
    [
        ["周一", "星期一", "礼拜一", "monday", "mon"],
        ["周二", "星期二", "礼拜二", "tuesday", "tue", "tues"],
        ["周三", "星期三", "礼拜三", "wednesday", "wed"],
        ["周四", "星期四", "礼拜四", "thursday", "thu", "thur", "thurs"],
        ["周五", "星期五", "礼拜五", "friday", "fri"],
        ["周六", "星期六", "礼拜六", "saturday", "sat"],
        ["周日", "周天", "星期日", "星期天", "礼拜日", "sunday", "sun"]
    ];

    // A conservative default used only when the screenshot explicitly supplies period numbers.
    // Users can correct school-specific times on the review screen before saving.
    private static readonly (string Start, string End)[] DefaultPeriods =
    [
        ("08.00am", "08.45am"), ("08.55am", "09.40am"),
        ("10.00am", "10.45am"), ("10.55am", "11.40am"),
        ("02.00pm", "02.45pm"), ("02.55pm", "03.40pm"),
        ("04.00pm", "04.45pm"), ("04.55pm", "05.40pm"),
        ("07.00pm", "07.45pm"), ("07.55pm", "08.40pm"),
        ("08.50pm", "09.35pm"), ("09.45pm", "10.30pm")
    ];

    public ScheduleImageParseResult Parse(OcrDocument document)
    {
        var result = new ScheduleImageParseResult();
        var regions = document.Regions
            .Where(x => !string.IsNullOrWhiteSpace(x.Text))
            .OrderBy(x => x.Top)
            .ThenBy(x => x.Left)
            .ToList();

        var headers = FindDayHeaders(regions);
        if (headers.Count < 2)
        {
            result.Warnings.Add("没有识别到足够的星期表头；请使用包含完整星期栏的网格课程表截图。");
            return result;
        }

        var headerBottom = headers.Max(x => x.Region.Bottom);
        var candidates = regions
            .Where(x => x.Top > headerBottom)
            .Where(x => FindDay(x.Text) is null)
            .ToList();

        foreach (var group in GroupIntoCards(candidates, headers, document.ImageHeight))
        {
            var parsed = ParseCard(group.Day, group.Regions);
            if (parsed is not null)
            {
                result.Courses.Add(parsed);
            }
        }

        if (result.Courses.Count == 0)
            result.Warnings.Add("识别到了星期栏，但没有找到可导入的课程卡片。请确认截图清晰且包含课程名称。");
        if (result.Courses.Any(x => !x.IsComplete))
            result.Warnings.Add("部分课程缺少准确时间。国内高校作息不同，请在保存前按本校时间校对红色字段。");

        return result;
    }

    private static List<(string Day, OcrTextRegion Region)> FindDayHeaders(IEnumerable<OcrTextRegion> regions) =>
        regions.Select(x => (Day: FindDay(x.Text), Region: x))
            .Where(x => x.Day is not null)
            .GroupBy(x => x.Day!)
            .Select(x => (Day: x.Key, Region: x.OrderBy(r => r.Region.Top).First().Region))
            .OrderBy(x => x.Region.CenterX)
            .ToList();

    private static string? FindDay(string text)
    {
        var normalized = Regex.Replace(text, @"\s+", "").ToLowerInvariant();
        for (var i = 0; i < DayAliases.Length; i++)
            if (DayAliases[i].Any(alias => normalized.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                                           normalized.Contains(alias, StringComparison.OrdinalIgnoreCase)))
                return Days[i];
        return null;
    }

    private static bool LooksLikeAxisLabel(string text)
    {
        var value = text.Trim();
        return TimeRangeRegex().IsMatch(value) ||
               PeriodOnlyRegex().IsMatch(value) ||
               Regex.IsMatch(value, @"^\d{1,2}[:：.]\d{2}$");
    }

    private static IEnumerable<(string Day, List<OcrTextRegion> Regions)> GroupIntoCards(
        List<OcrTextRegion> candidates,
        List<(string Day, OcrTextRegion Region)> headers,
        int imageHeight)
    {
        var typicalHeight = candidates.Count == 0 ? 24f : candidates.Select(x => x.Height).Order().ElementAt(candidates.Count / 2);
        var verticalGap = Math.Max(typicalHeight * 1.8f, imageHeight * 0.018f);

        foreach (var header in headers)
        {
            var orderedHeaders = headers.OrderBy(x => x.Region.CenterX).ToList();
            var index = orderedHeaders.FindIndex(x => x.Day == header.Day);
            var left = index == 0
                ? header.Region.CenterX - (orderedHeaders[1].Region.CenterX - header.Region.CenterX) / 2f
                : (orderedHeaders[index - 1].Region.CenterX + header.Region.CenterX) / 2f;
            var right = index == orderedHeaders.Count - 1
                ? header.Region.CenterX + (header.Region.CenterX - orderedHeaders[index - 1].Region.CenterX) / 2f
                : (header.Region.CenterX + orderedHeaders[index + 1].Region.CenterX) / 2f;
            var column = candidates.Where(x => x.CenterX >= left && x.CenterX < right).OrderBy(x => x.Top).ToList();
            var card = new List<OcrTextRegion>();

            foreach (var region in column)
            {
                if (card.Count > 0 && region.Top - card.Max(x => x.Bottom) > verticalGap)
                {
                    yield return (header.Day, card);
                    card = [];
                }
                card.Add(region);
            }
            if (card.Count > 0) yield return (header.Day, card);
        }
    }

    private static RecognizedCourse? ParseCard(string day, List<OcrTextRegion> regions)
    {
        var lines = regions.Select(x => x.Text.Trim()).Where(x => x.Length > 0).ToList();
        if (lines.Count == 0) return null;
        var source = string.Join(" ", lines);
        var (start, end, timeConfidence) = ParseTime(source);

        var content = lines
            .Where(x => !TimeRangeRegex().IsMatch(x) && !PeriodOnlyRegex().IsMatch(x))
            .Where(x => !Regex.IsMatch(x, @"^(第?\d+[~-]\d+周|\d+[单双]?周)$"))
            .ToList();
        if (content.Count == 0) return null;

        var location = content.FirstOrDefault(IsLocation) ?? "";
        var teacher = content.FirstOrDefault(IsTeacher) ?? "";
        var name = content.FirstOrDefault(x => x != location && x != teacher) ?? content[0];
        name = CleanLabel(name, "课程", "课程名", "名称");
        location = CleanLabel(location, "地点", "教室", "上课地点");
        teacher = CleanLabel(teacher, "教师", "老师", "授课教师");

        if (name.Length < 2 || Regex.IsMatch(name, @"^\d+$")) return null;
        return new RecognizedCourse
        {
            Name = name,
            Location = location,
            Teacher = teacher,
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            Confidence = Math.Clamp(0.55 + timeConfidence + (location.Length > 0 ? 0.08 : 0), 0, 0.98),
            SourceText = source
        };
    }

    private static (string Start, string End, double Confidence) ParseTime(string text)
    {
        var match = TimeRangeRegex().Match(text);
        if (match.Success && TryFormatTime(match.Groups["start"].Value, out var start) &&
            TryFormatTime(match.Groups["end"].Value, out var end))
            return (start, end, 0.30);

        var period = PeriodRangeRegex().Match(text);
        if (period.Success && int.TryParse(period.Groups["start"].Value, out var first) &&
            int.TryParse(period.Groups["end"].Value, out var last) && first >= 1 && last <= DefaultPeriods.Length && first <= last)
            return (DefaultPeriods[first - 1].Start, DefaultPeriods[last - 1].End, 0.12);

        return ("", "", 0);
    }

    private static bool TryFormatTime(string raw, out string value)
    {
        value = "";
        var normalized = raw.Replace('：', ':').Replace('.', ':').Trim();
        if (!TimeSpan.TryParseExact(normalized, [@"h\:mm", @"hh\:mm"], CultureInfo.InvariantCulture, out var time)) return false;
        var hour = time.Hours % 12;
        if (hour == 0) hour = 12;
        value = $"{hour:00}.{time.Minutes:00}{(time.Hours >= 12 ? "pm" : "am")}";
        return true;
    }

    private static bool IsLocation(string value) =>
        Regex.IsMatch(value, @"(教室|地点|楼|室|校区|馆|实验中心|[A-Za-z]\d{2,4})", RegexOptions.IgnoreCase);

    private static bool IsTeacher(string value) =>
        Regex.IsMatch(value, @"(教师|老师|讲师|教授|副教授|助教)");

    private static string CleanLabel(string value, params string[] labels)
    {
        foreach (var label in labels)
            value = Regex.Replace(value, $@"^{Regex.Escape(label)}\s*[:：]?\s*", "", RegexOptions.IgnoreCase);
        return value.Trim(' ', '|', '·');
    }

    [GeneratedRegex(@"(?<start>(?:[01]?\d|2[0-3])[:：.]\d{2})\s*(?:-|—|–|~|～|至)\s*(?<end>(?:[01]?\d|2[0-3])[:：.]\d{2})")]
    private static partial Regex TimeRangeRegex();

    [GeneratedRegex(@"(?:第\s*)?(?<start>\d{1,2})\s*(?:-|—|–|~|～|至)\s*(?<end>\d{1,2})\s*节")]
    private static partial Regex PeriodRangeRegex();

    [GeneratedRegex(@"^(?:第\s*)?\d{1,2}\s*(?:-|—|–|~|～|至)?\s*\d{0,2}\s*节$")]
    private static partial Regex PeriodOnlyRegex();
}
