using System.Globalization;
using System.Text;
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
        ["周四", "星期四", "礼拜四", "thursday", "thu", "thur", "thurs", "thus"],
        ["周五", "星期五", "礼拜五", "friday", "fri"],
        ["周六", "星期六", "礼拜六", "saturday", "sat"],
        ["周日", "周天", "星期日", "星期天", "礼拜日", "sunday", "sun"]
    ];

    private static readonly Dictionary<string, int> SingleDayAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["一"] = 0, ["二"] = 1, ["三"] = 2, ["四"] = 3, ["五"] = 4,
        ["六"] = 5, ["日"] = 6, ["天"] = 6
    };

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

        var headers = FindDayHeaders(regions, document.ImageHeight);
        if (headers.Count < 2)
        {
            result.Warnings.Add("没有识别到足够的星期表头；请使用包含完整星期栏的课程表截图。");
            result.IsWriteSafe = false;
            return result;
        }

        var columns = BuildDayColumns(headers, document.VerticalLines, document.ImageWidth);
        var firstColumnLeft = columns.Min(x => x.Left);
        var headerBottom = headers.Max(x => x.Region.Bottom);
        var timeAxis = FindTimeAxis(regions, firstColumnLeft, headerBottom, document.HorizontalLines);
        var headerRegions = headers.Select(x => x.Region).ToHashSet();
        var candidates = regions
            .Where(x => x.Top > headerBottom)
            .Where(x => !headerRegions.Contains(x))
            .Where(x => x.CenterX >= firstColumnLeft)
            .ToList();

        foreach (var group in GroupIntoCards(candidates, columns, document.ImageHeight))
        {
            foreach (var entry in SplitCard(group.Regions))
            {
                var parsed = ParseCard(group.Day, entry, timeAxis, document.HorizontalLines);
                if (parsed is not null) result.Courses.Add(parsed);
            }
        }

        RemoveObviousDuplicates(result.Courses);
        EvaluateQuality(result, headers.Count);
        return result;
    }

    private static List<DayHeader> FindDayHeaders(List<OcrTextRegion> regions, int imageHeight)
    {
        var tokens = regions
            .Select(x => TryGetDayToken(x, out var day, out var strength)
                ? new DayHeader(day, x, strength)
                : null)
            .Where(x => x is not null)
            .Cast<DayHeader>()
            .OrderBy(x => x.Region.CenterY)
            .ToList();
        if (tokens.Count == 0) return [];

        var medianHeight = regions.Count == 0
            ? 20f
            : regions.Select(x => x.Height).Order().ElementAt(regions.Count / 2);
        var tolerance = Math.Max(medianHeight * 1.8f, imageHeight * 0.012f);
        var clusters = new List<List<DayHeader>>();
        foreach (var token in tokens)
        {
            var cluster = clusters.LastOrDefault(x => Math.Abs(x.Average(y => y.Region.CenterY) - token.Region.CenterY) <= tolerance);
            if (cluster is null)
                clusters.Add([token]);
            else
                cluster.Add(token);
        }

        return clusters
            .Select(cluster => cluster
                .GroupBy(x => x.Day)
                .Select(group => group.OrderByDescending(x => x.Strength).ThenBy(x => x.Region.Top).First())
                .OrderBy(x => x.Region.CenterX)
                .ToList())
            .Where(cluster => cluster.Count >= 2)
            .OrderByDescending(cluster => cluster.Count * 20 + cluster.Sum(x => x.Strength))
            .ThenBy(cluster => cluster.Average(x => x.Region.Top))
            .FirstOrDefault() ?? [];
    }

    private static bool TryGetDayToken(OcrTextRegion region, out string day, out int strength)
    {
        day = "";
        strength = 0;
        var normalized = Regex.Replace(region.Text, @"\s+", "").Trim('：', ':').ToLowerInvariant();
        for (var i = 0; i < DayAliases.Length; i++)
        {
            if (!DayAliases[i].Any(alias => normalized.Equals(alias, StringComparison.OrdinalIgnoreCase) ||
                                             normalized.Contains(alias, StringComparison.OrdinalIgnoreCase))) continue;
            day = Days[i];
            strength = 3;
            return true;
        }

        if (!SingleDayAliases.TryGetValue(normalized, out var index)) return false;
        day = Days[index];
        strength = 1;
        return true;
    }

    private static List<DayColumn> BuildDayColumns(
        List<DayHeader> headers,
        IReadOnlyList<float> verticalLines,
        int imageWidth)
    {
        var ordered = headers.OrderBy(x => x.Region.CenterX).ToList();
        var gridLines = verticalLines.Order().ToList();
        var columns = new List<DayColumn>();
        for (var i = 0; i < ordered.Count; i++)
        {
            var center = ordered[i].Region.CenterX;
            var gridLeft = gridLines.Where(x => x < center - 1).DefaultIfEmpty(float.NaN).Max();
            var gridRight = gridLines.Where(x => x > center + 1).DefaultIfEmpty(float.NaN).Min();
            var gridWidth = gridRight - gridLeft;
            var useGrid = !float.IsNaN(gridLeft) && !float.IsNaN(gridRight) &&
                          gridWidth >= imageWidth * 0.035f && gridWidth <= imageWidth * 0.32f;

            var left = useGrid
                ? gridLeft
                : i == 0
                    ? center - (ordered[i + 1].Region.CenterX - center) / 2f
                    : (ordered[i - 1].Region.CenterX + center) / 2f;
            var right = useGrid
                ? gridRight
                : i == ordered.Count - 1
                    ? center + (center - ordered[i - 1].Region.CenterX) / 2f
                    : (center + ordered[i + 1].Region.CenterX) / 2f;
            columns.Add(new DayColumn(ordered[i].Day, Math.Max(0, left), Math.Min(imageWidth, right)));
        }
        return columns;
    }

    private static List<TimeAxisEntry> FindTimeAxis(
        List<OcrTextRegion> regions,
        float firstColumnLeft,
        float headerBottom,
        IReadOnlyList<float> horizontalLines)
    {
        var axis = regions
            .Where(x => x.CenterX < firstColumnLeft && x.Top > headerBottom)
            .OrderBy(x => x.Top)
            .ToList();
        if (axis.Count == 0) return [];

        var lines = horizontalLines.Where(x => x > headerBottom).Order().ToList();
        var entries = new List<TimeAxisEntry>();
        if (lines.Count >= 2)
        {
            for (var i = 0; i < lines.Count - 1; i++)
            {
                var top = lines[i];
                var bottom = lines[i + 1];
                var inBand = axis.Where(x => x.CenterY > top && x.CenterY < bottom).ToList();
                var entry = BuildTimeAxisEntry(inBand, top, bottom);
                if (entry is not null) entries.Add(entry);
            }
        }

        if (entries.Count > 0) return entries;
        foreach (var line in MergeElementsIntoLines(axis, 1000))
        {
            var entry = BuildTimeAxisEntry([line], line.Top, line.Bottom);
            if (entry is not null) entries.Add(entry);
        }
        return entries;
    }

    private static TimeAxisEntry? BuildTimeAxisEntry(List<OcrTextRegion> regions, float top, float bottom)
    {
        if (regions.Count == 0) return null;
        var source = string.Join(" ", regions.OrderBy(x => x.Top).ThenBy(x => x.Left).Select(x => x.Text));
        var times = SingleTimeRegex().Matches(source)
            .Select(x => x.Value)
            .Where(x => TryFormatFlexibleTime(x, out _))
            .ToList();
        string start = "", end = "";
        if (times.Count > 0) TryFormatFlexibleTime(times[0], out start);
        if (times.Count > 1) TryFormatFlexibleTime(times[^1], out end);

        var periodMatches = PeriodNumberRegex().Matches(source).Select(x => x.Groups["number"].Value).ToList();
        var periods = periodMatches.Select(x => int.TryParse(x, out var number) ? number : 0).Where(x => x > 0).ToList();
        if (start.Length == 0 && periods.Count == 0) return null;
        return new TimeAxisEntry(top, bottom, start, end, periods.FirstOrDefault(), periods.LastOrDefault());
    }

    private static IEnumerable<CardGroup> GroupIntoCards(
        List<OcrTextRegion> candidates,
        List<DayColumn> columns,
        int imageHeight)
    {
        foreach (var columnInfo in columns)
        {
            var elements = candidates
                .Where(x => x.CenterX >= columnInfo.Left && x.CenterX < columnInfo.Right)
                .ToList();
            var column = MergeElementsIntoLines(elements, imageHeight);
            if (column.Count == 0) continue;
            var typicalHeight = column.Select(x => x.Height).Order().ElementAt(column.Count / 2);
            var verticalGap = Math.Max(typicalHeight * 1.65f, imageHeight * 0.012f);
            var card = new List<OcrTextRegion>();
            foreach (var region in column.OrderBy(x => x.Top))
            {
                if (card.Count > 0 && region.Top - card.Max(x => x.Bottom) > verticalGap)
                {
                    yield return new CardGroup(columnInfo.Day, card);
                    card = [];
                }
                card.Add(region);
            }
            if (card.Count > 0) yield return new CardGroup(columnInfo.Day, card);
        }
    }

    private static List<OcrTextRegion> MergeElementsIntoLines(List<OcrTextRegion> regions, int imageHeight)
    {
        if (regions.Count == 0) return [];
        var medianHeight = regions.Select(x => x.Height).Order().ElementAt(regions.Count / 2);
        var tolerance = Math.Max(medianHeight * 0.65f, imageHeight * 0.0025f);
        var groups = new List<List<OcrTextRegion>>();
        foreach (var region in regions.OrderBy(x => x.CenterY).ThenBy(x => x.Left))
        {
            var group = groups.LastOrDefault(x => Math.Abs(x.Average(y => y.CenterY) - region.CenterY) <= tolerance);
            if (group is null)
                groups.Add([region]);
            else
                group.Add(region);
        }

        return groups.Select(group =>
        {
            var ordered = group.OrderBy(x => x.Left).ToList();
            return new OcrTextRegion(
                JoinTokens(ordered.Select(x => x.Text)),
                ordered.Min(x => x.Left), ordered.Min(x => x.Top),
                ordered.Max(x => x.Right), ordered.Max(x => x.Bottom));
        }).OrderBy(x => x.Top).ToList();
    }

    private static string JoinTokens(IEnumerable<string> values)
    {
        var result = new StringBuilder();
        foreach (var value in values.Select(x => x.Trim()).Where(x => x.Length > 0))
        {
            if (result.Length > 0) result.Append(' ');
            result.Append(value);
        }
        return result.ToString();
    }

    private static IEnumerable<List<OcrTextRegion>> SplitCard(List<OcrTextRegion> regions)
    {
        var current = new List<OcrTextRegion>();
        foreach (var region in regions.OrderBy(x => x.Top).ThenBy(x => x.Left))
        {
            if (current.Count > 0 && BracketedCourseRegex().IsMatch(region.Text))
            {
                yield return current;
                current = [];
            }
            current.Add(region);
        }
        if (current.Count > 0) yield return current;
    }

    private static RecognizedCourse? ParseCard(
        string day,
        List<OcrTextRegion> regions,
        List<TimeAxisEntry> timeAxis,
        IReadOnlyList<float> horizontalLines)
    {
        var ordered = regions.OrderBy(x => x.Top).ThenBy(x => x.Left).ToList();
        var lines = ordered.Select(x => NormalizeWhitespace(x.Text)).Where(x => x.Length > 0).ToList();
        if (lines.Count == 0) return null;
        var source = string.Join(" ", lines);
        if (IsAxisOnly(source)) return null;

        var (start, end, timeConfidence) = ParseTime(source, timeAxis);
        if (start.Length == 0)
            (start, end, timeConfidence) = ParseTimeFromGrid(ordered, timeAxis, horizontalLines);

        var location = lines.Select(ExtractLocation).FirstOrDefault(x => x.Length > 0) ?? "";
        var teacher = lines.Select(ExtractExplicitTeacher).FirstOrDefault(x => x.Length > 0) ?? "";
        var name = ExtractBracketedCourseName(source);

        var meaningful = lines
            .Select((text, index) => new FieldLine(text, index))
            .Where(x => !IsPureMetadata(x.Text) && !IsAxisOnly(x.Text))
            .ToList();
        if (name.Length == 0)
        {
            var code = meaningful.FirstOrDefault(x => IsCourseCode(x.Text));
            if (code is not null)
            {
                name = meaningful.FirstOrDefault(x => x.Index > code.Index && !IsLocation(x.Text))?.Text ?? "";
            }
        }

        if (name.Length == 0)
        {
            foreach (var line in meaningful)
            {
                var combined = ExtractCombinedFields(line.Text);
                if (combined.Name.Length == 0) continue;
                name = combined.Name;
                if (teacher.Length == 0) teacher = combined.Teacher;
                if (location.Length == 0) location = combined.Location;
                break;
            }
        }

        name = CleanCourseName(name);
        if (name.Length < 2 || IsAxisOnly(name) || IsPureMetadata(name)) return null;

        if (teacher.Length == 0)
        {
            var nameIndex = lines.FindIndex(x => x.Contains(name, StringComparison.OrdinalIgnoreCase));
            var locationIndex = lines.FindIndex(IsLocation);
            teacher = lines
                .Skip(Math.Max(0, nameIndex + 1))
                .Take(locationIndex > nameIndex ? locationIndex - nameIndex - 1 : lines.Count)
                .FirstOrDefault(IsLikelyChinesePerson) ?? "";
        }
        if (teacher.Length == 0)
        {
            teacher = lines.FirstOrDefault(x =>
                !x.Contains(name, StringComparison.OrdinalIgnoreCase) &&
                !IsLocation(x) && !IsPureMetadata(x) && !IsCourseCode(x) &&
                (IsLikelyChinesePerson(x) || IsLikelyEnglishPerson(x))) ?? "";
        }

        var confidence = 0.48 + timeConfidence;
        if (location.Length > 0) confidence += 0.10;
        if (teacher.Length > 0) confidence += 0.08;
        if (BracketedCourseRegex().IsMatch(source)) confidence += 0.08;

        return new RecognizedCourse
        {
            Name = name,
            Location = CleanField(location),
            Teacher = CleanTeacher(teacher),
            DayOfWeek = day,
            StartTime = start,
            EndTime = end,
            Confidence = Math.Clamp(confidence, 0, 0.98),
            SourceText = source
        };
    }

    private static CombinedFields ExtractCombinedFields(string text)
    {
        var location = ExtractLocation(text);
        var working = location.Length == 0 ? text : text.Replace(location, " ", StringComparison.OrdinalIgnoreCase);
        var metadata = MetadataStartRegex().Match(working);
        if (metadata.Success) working = working[..metadata.Index];
        working = StripCourseCode(working).Trim(' ', '|', ',', '，', '。');
        var tokens = working.Split(' ', StringSplitOptions.RemoveEmptyEntries).ToList();
        var teacher = "";
        if (tokens.Count >= 2 && IsLikelyChinesePerson(tokens[^1]))
        {
            teacher = tokens[^1];
            tokens.RemoveAt(tokens.Count - 1);
        }
        return new CombinedFields(string.Join(" ", tokens), teacher, location);
    }

    private static string ExtractBracketedCourseName(string source)
    {
        var match = BracketedCourseRegex().Match(source);
        return match.Success ? match.Groups["name"].Value : "";
    }

    private static string ExtractLocation(string text)
    {
        var tokens = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        foreach (var token in tokens.Reverse())
        {
            var cleaned = token.Trim(',', '，', '。', ')', '）', '(', '（');
            if (IsLocation(cleaned)) return cleaned;
        }
        return IsLocation(text) ? text.Trim() : "";
    }

    private static bool IsLocation(string value)
    {
        var compact = Regex.Replace(value, @"\s+", "");
        if (IsCourseCode(compact)) return false;
        return Regex.IsMatch(compact, @"(?:教室|教学楼|教学大楼|实验楼|实验室|实训楼|特教楼|科研楼|主楼|楼|校区|中心|馆|室)", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(compact, @"^[A-Za-z0-9ⅠⅡⅢⅣ一二三四五六七八九十]{1,8}[-#][A-Za-z0-9#-]{1,10}$", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(compact, @"^[\p{IsCJKUnifiedIdeographs}A-Za-zⅠⅡⅢⅣ]{1,8}[A-Za-z]?[#-]?\d{2,4}(?:室)?$", RegexOptions.IgnoreCase);
    }

    private static string ExtractExplicitTeacher(string text)
    {
        var suffix = Regex.Match(text, @"^(?<name>[\p{IsCJKUnifiedIdeographs}·]{2,4}(?:老师|教师))$");
        if (suffix.Success) return suffix.Groups["name"].Value;
        var match = ExplicitTeacherRegex().Match(text);
        if (!match.Success) return "";
        var value = match.Groups["name"].Success ? match.Groups["name"].Value : match.Value;
        return CleanTeacher(value);
    }

    private static bool IsLikelyChinesePerson(string value)
    {
        var compact = Regex.Replace(value, @"\s+", "").Trim('(', ')', '（', '）');
        if (!Regex.IsMatch(compact, @"^[\p{IsCJKUnifiedIdeographs}·]{2,4}(?:老师|教师)?$")) return false;
        return !Regex.IsMatch(compact, @"(?:大学|学院|英语|数学|物理|化学|基础|原理|概论|技术|系统|管理|课程|实践|实验|体育|政策|文学|教育|研究|方法|设计|分析|工程|专题)$");
    }

    private static bool IsLikelyEnglishPerson(string value)
    {
        if (value.Any(char.IsDigit) || IsLocation(value)) return false;
        var words = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return words.Length is >= 2 and <= 8 &&
               words.All(x => Regex.IsMatch(x.Trim('.', ',', '\'', '-'), @"^[A-Za-z]+$"));
    }

    private static string CleanCourseName(string value)
    {
        value = StripCourseCode(value);
        value = MetadataStartRegex().Replace(value, " ");
        value = Regex.Replace(value, @"^[\[【]|[\]】]$", "");
        value = Regex.Replace(value, @"\s+", " ").Trim(' ', '|', ',', '，', '。', '(', ')', '（', '）');
        return value;
    }

    private static string StripCourseCode(string value) =>
        Regex.Replace(value, @"^\s*[A-Za-z]{2,}[A-Za-z0-9._-]*\d[A-Za-z0-9._-]*\s*", "", RegexOptions.IgnoreCase);

    private static string CleanTeacher(string value) =>
        Regex.Replace(value, @"^(?:教师|老师|讲师|教授|teacher|lecturer|professor|instructor)\s*[:：]?\s*", "", RegexOptions.IgnoreCase).Trim();

    private static string CleanField(string value) => value.Trim(' ', '|', ',', '，', '。', '(', ')', '（', '）');

    private static string NormalizeWhitespace(string value) => Regex.Replace(value.Trim(), @"\s+", " ");

    private static bool IsAxisOnly(string text)
    {
        var compact = Regex.Replace(text, @"\s+", "");
        return Regex.IsMatch(compact, @"^(?:上|下|晚|第|#)?\d{1,2}(?:\[?\d{1,2}[:：.]\d{2}-?\]?)?$", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(compact, @"^\d{1,2}[:：.]\d{2}(?:am|pm)?(?:-|—|–|~|～)?$", RegexOptions.IgnoreCase);
    }

    private static bool IsPureMetadata(string value)
    {
        var compact = Regex.Replace(value, @"\s+", "");
        return WeekOnlyRegex().IsMatch(compact) || PeriodOnlyRegex().IsMatch(compact) ||
               Regex.IsMatch(compact, @"^(?:星期|周)[一二三四五六日天1-7]$");
    }

    private static bool IsCourseCode(string value)
    {
        var compact = Regex.Replace(value, @"\s+", "").Trim('(', ')', '（', '）');
        return compact.Length <= 22 && compact.Any(char.IsLetter) && compact.Any(char.IsDigit) &&
               Regex.IsMatch(compact, @"^[A-Za-z]{2,}[A-Za-z0-9._-]*\d[A-Za-z0-9._-]*$", RegexOptions.IgnoreCase);
    }

    private static (string Start, string End, double Confidence) ParseTime(
        string text,
        List<TimeAxisEntry> timeAxis)
    {
        var match = TimeRangeRegex().Match(text);
        if (match.Success && TryFormatFlexibleTime(match.Groups["start"].Value, out var start) &&
            TryFormatFlexibleTime(match.Groups["end"].Value, out var end))
            return (start, end, 0.30);

        var period = PeriodRangeRegex().Match(text);
        if (!period.Success || !int.TryParse(period.Groups["start"].Value, out var first) ||
            !int.TryParse(period.Groups["end"].Value, out var last) || first < 1 || first > last)
            return ("", "", 0);

        var firstAxis = timeAxis.FirstOrDefault(x => x.FirstPeriod > 0 && first >= x.FirstPeriod && first <= Math.Max(x.LastPeriod, x.FirstPeriod));
        var lastAxis = timeAxis.LastOrDefault(x => x.FirstPeriod > 0 && last >= x.FirstPeriod && last <= Math.Max(x.LastPeriod, x.FirstPeriod));
        if (firstAxis is not null && lastAxis is not null && firstAxis.Start.Length > 0 && lastAxis.End.Length > 0)
            return (firstAxis.Start, lastAxis.End, 0.28);

        if (last <= DefaultPeriods.Length)
            return (DefaultPeriods[first - 1].Start, DefaultPeriods[last - 1].End, 0.08);
        return ("", "", 0);
    }

    private static (string Start, string End, double Confidence) ParseTimeFromGrid(
        List<OcrTextRegion> regions,
        List<TimeAxisEntry> timeAxis,
        IReadOnlyList<float> horizontalLines)
    {
        if (timeAxis.Count == 0) return ("", "", 0);
        var textTop = regions.Min(x => x.Top);
        var textBottom = regions.Max(x => x.Bottom);
        var overlapping = timeAxis
            .Where(x => x.Bottom > textTop && x.Top < textBottom && x.Start.Length > 0)
            .OrderBy(x => x.Top)
            .ToList();
        if (overlapping.Count > 0)
        {
            var lastEnd = overlapping.LastOrDefault(x => x.End.Length > 0)?.End ?? "";
            if (lastEnd.Length > 0)
                return (overlapping[0].Start, lastEnd, overlapping.Count > 1 ? 0.28 : 0.22);
        }

        var center = regions.Average(x => x.CenterY);
        var entry = timeAxis.FirstOrDefault(x => center > x.Top && center < x.Bottom) ??
                    timeAxis.MinBy(x => Math.Abs((x.Top + x.Bottom) / 2f - center));
        if (entry is null || entry.Start.Length == 0) return ("", "", 0);
        var end = entry.End;
        if (end.Length == 0 && entry.FirstPeriod > 0 && entry.FirstPeriod <= DefaultPeriods.Length)
            end = DefaultPeriods[Math.Min(DefaultPeriods.Length, Math.Max(entry.LastPeriod, entry.FirstPeriod)) - 1].End;
        return (entry.Start, end, end.Length > 0 ? 0.22 : 0.08);
    }

    private static bool TryFormatFlexibleTime(string raw, out string value)
    {
        value = "";
        var normalized = raw.Replace('：', ':').Replace('.', ':').Replace(" ", "").ToLowerInvariant();
        var formats = new[] { "H:mm", "HH:mm", "h:mmtt", "hh:mmtt" };
        if (!DateTime.TryParseExact(normalized, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time))
            return false;
        value = time.ToString("hh.mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();
        return true;
    }

    private static void RemoveObviousDuplicates(List<RecognizedCourse> courses)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (var i = courses.Count - 1; i >= 0; i--)
        {
            var course = courses[i];
            var key = $"{course.DayOfWeek}|{course.StartTime}|{course.EndTime}|{Regex.Replace(course.Name, @"\s+", "")}";
            if (!seen.Add(key)) courses.RemoveAt(i);
        }
    }

    private static void EvaluateQuality(ScheduleImageParseResult result, int headerCount)
    {
        if (result.Courses.Count == 0)
        {
            result.Warnings.Add("识别到了星期栏，但没有找到可导入的课程区域。");
            result.IsWriteSafe = false;
            return;
        }

        var suspicious = result.Courses.Count(x => IsAxisOnly(x.Name) || IsPureMetadata(x.Name) || x.Name.Length > 80);
        var completeRatio = result.Courses.Count(x => x.IsComplete) / (double)result.Courses.Count;
        var metadataRatio = result.Courses.Count(x =>
            !string.IsNullOrWhiteSpace(x.Teacher) || !string.IsNullOrWhiteSpace(x.Location)) /
            (double)result.Courses.Count;
        var oneDayConcentration = headerCount >= 5 && result.Courses.Count >= 3 &&
                                  result.Courses.Select(x => x.DayOfWeek).Distinct().Count() == 1;
        var weakStructure = headerCount < 5 || (result.Courses.Count >= 3 && metadataRatio < 0.50);
        result.IsWriteSafe = suspicious == 0 && completeRatio >= 0.70 && !oneDayConcentration &&
                             !weakStructure && result.Courses.Average(x => x.Confidence) >= 0.58;

        if (result.Courses.Any(x => !x.IsComplete))
            result.Warnings.Add("部分课程缺少准确时间，请在保存前根据本校作息校对。");
        if (suspicious > 0 || oneDayConcentration || weakStructure)
            result.Warnings.Add("识别结果存在明显的结构异常，已禁止直接批量写入。");
        else if (!result.IsWriteSafe)
            result.Warnings.Add("识别结果整体置信度较低，已禁止直接批量写入。");
    }

    private sealed record DayHeader(string Day, OcrTextRegion Region, int Strength);
    private sealed record DayColumn(string Day, float Left, float Right);
    private sealed record CardGroup(string Day, List<OcrTextRegion> Regions);
    private sealed record TimeAxisEntry(float Top, float Bottom, string Start, string End, int FirstPeriod, int LastPeriod);
    private sealed record FieldLine(string Text, int Index);
    private sealed record CombinedFields(string Name, string Teacher, string Location);

    [GeneratedRegex(@"[\[【](?<name>[^\]】]{2,80})[\]】]")]
    private static partial Regex BracketedCourseRegex();

    [GeneratedRegex(@"(?<start>(?:[01]?\d|2[0-3])[:：.]\d{2}(?:\s*[ap]m)?)\s*(?:-|—|–|~|～|至)\s*(?<end>(?:[01]?\d|2[0-3])[:：.]\d{2}(?:\s*[ap]m)?)", RegexOptions.IgnoreCase)]
    private static partial Regex TimeRangeRegex();

    [GeneratedRegex(@"(?:[01]?\d|2[0-3])[:：.]\d{2}(?:\s*[ap]m)?", RegexOptions.IgnoreCase)]
    private static partial Regex SingleTimeRegex();

    [GeneratedRegex(@"(?:第\s*)?(?<start>\d{1,2})\s*(?:-|—|–|~|～|至)\s*(?<end>\d{1,2})\s*节")]
    private static partial Regex PeriodRangeRegex();

    [GeneratedRegex(@"(?:第|上|下|晚|#)?\s*(?<number>\d{1,2})\s*节?")]
    private static partial Regex PeriodNumberRegex();

    [GeneratedRegex(@"^(?:第)?\d{1,2}(?:[-—–~～至]\d{1,2})?节$")]
    private static partial Regex PeriodOnlyRegex();

    [GeneratedRegex(@"^(?:\(?\d+(?:[-—–~～至,，、]\d+)*周(?:单双)?\)?|\(?week\s*\d+(?:[-—–~～至]\d+)?\)?)$", RegexOptions.IgnoreCase)]
    private static partial Regex WeekOnlyRegex();

    [GeneratedRegex(@"(?:\d+(?:[-—–~～至,，、]\d+)*周|week\s*\d+|星期[一二三四五六日天1-7]|第\s*\d+(?:[-—–~～至]\d+)?\s*节)", RegexOptions.IgnoreCase)]
    private static partial Regex MetadataStartRegex();

    [GeneratedRegex(@"(?:教师|老师|讲师|教授|teacher|lecturer|professor|instructor)\s*[:：]?\s*(?<name>[\p{L}·.' -]{2,40})", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitTeacherRegex();
}
