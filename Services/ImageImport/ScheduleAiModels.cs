using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace Schedule2._0.Services.ImageImport;

public interface IScheduleAiService
{
    bool IsSupported { get; }
    bool IsModelInstalled { get; }
    string ModelFileName { get; }
    Task InstallModelAsync(Stream source, CancellationToken cancellationToken = default);
    Task<string> StructureAsync(OcrDocument document, CancellationToken cancellationToken = default);
}

public sealed class UnsupportedScheduleAiService : IScheduleAiService
{
    public bool IsSupported => false;
    public bool IsModelInstalled => false;
    public string ModelFileName => "MiniCPM5-1B-Schedule-Q4_K_M.gguf";
    public Task InstallModelAsync(Stream source, CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException("课程表 AI 结构化目前仅支持 Android arm64。 ");
    public Task<string> StructureAsync(OcrDocument document, CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException("课程表 AI 结构化目前仅支持 Android arm64。 ");
}

public static class OcrLayoutSerializer
{
    public const string Instruction =
        "你是课程表 OCR 版面结构化器。输入是离线 OCR 得到的文本块，每行格式为 [x0,y0,x1,y1] 文本，坐标已归一化到 0..1000。" +
        "根据星期表头、时间/节次轴和文本块二维位置恢复课程。不要猜测不存在的信息；同一视觉课程块只输出一条。" +
        "startTime/endTime 必须是 HH:mm。若不是个人周课程表，输出 other 和空 courses。" +
        "只输出合法紧凑 JSON，字段固定为 schemaVersion、documentType、courses；" +
        "课程字段固定为 name、teacher、location、dayOfWeek、startPeriod、endPeriod、startTime、endTime、weeks。";

    public static string Serialize(OcrDocument document)
    {
        if (document.ImageWidth <= 0 || document.ImageHeight <= 0)
            throw new ArgumentException("OCR 图片尺寸无效。", nameof(document));

        var builder = new StringBuilder(Instruction).AppendLine().AppendLine("OCR_BLOCKS:");
        foreach (var region in document.Regions
                     .Where(x => !string.IsNullOrWhiteSpace(x.Text))
                     .OrderBy(x => x.Top).ThenBy(x => x.Left))
        {
            static int Normalize(float value, int size) => Math.Clamp((int)Math.Round(value * 1000d / size), 0, 1000);
            var text = Regex.Replace(region.Text, @"[\r\n]+", " ").Trim();
            builder.Append('[')
                .Append(Normalize(region.Left, document.ImageWidth)).Append(',')
                .Append(Normalize(region.Top, document.ImageHeight)).Append(',')
                .Append(Normalize(region.Right, document.ImageWidth)).Append(',')
                .Append(Normalize(region.Bottom, document.ImageHeight)).Append("] ")
                .AppendLine(text);
        }
        return builder.ToString().TrimEnd();
    }

    public static string ToChatPrompt(OcrDocument document) =>
        $"<|im_start|>user\n{Serialize(document)}<|im_end|>\n<|im_start|>assistant\n<think>\n\n</think>\n\n";
}

public sealed class AiScheduleDocument
{
    [JsonPropertyName("schemaVersion")] public int SchemaVersion { get; set; }
    [JsonPropertyName("documentType")] public string DocumentType { get; set; } = "";
    [JsonPropertyName("courses")] public List<AiScheduleCourse> Courses { get; set; } = [];
}

public sealed class AiScheduleCourse
{
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("teacher")] public string Teacher { get; set; } = "";
    [JsonPropertyName("location")] public string Location { get; set; } = "";
    [JsonPropertyName("dayOfWeek")] public string DayOfWeek { get; set; } = "";
    [JsonPropertyName("startPeriod")] public int StartPeriod { get; set; }
    [JsonPropertyName("endPeriod")] public int EndPeriod { get; set; }
    [JsonPropertyName("startTime")] public string StartTime { get; set; } = "";
    [JsonPropertyName("endTime")] public string EndTime { get; set; } = "";
    [JsonPropertyName("weeks")] public JsonElement Weeks { get; set; }
}

public static class AiScheduleJsonParser
{
    private static readonly JsonSerializerOptions Options = new() { PropertyNameCaseInsensitive = false };

    public static AiScheduleDocument Parse(string output)
    {
        var candidate = Regex.Replace(output.Trim(), @"<think>[\s\S]*?</think>\s*", "").Trim();
        candidate = Regex.Replace(candidate, @"^```(?:json)?\s*|\s*```$", "", RegexOptions.IgnoreCase);
        var start = candidate.IndexOf('{');
        var end = candidate.LastIndexOf('}');
        AiScheduleDocument parsed;
        try
        {
            if (start < 0 || end <= start) throw new JsonException();
            parsed = JsonSerializer.Deserialize<AiScheduleDocument>(candidate[start..(end + 1)], Options)
                     ?? throw new JsonException();
        }
        catch (JsonException)
        {
            parsed = RecoverCompleteCourseObjects(candidate);
        }
        if (parsed.SchemaVersion != 1) throw new InvalidDataException("本地模型返回了不支持的数据版本。 ");
        if (parsed.Courses.Count > 60) throw new InvalidDataException("模型输出课程数量异常，已拒绝导入。 ");
        return parsed;
    }

    private static AiScheduleDocument RecoverCompleteCourseObjects(string candidate)
    {
        if (!candidate.Contains("\"documentType\":\"weekly_schedule\"", StringComparison.Ordinal))
            throw new InvalidDataException("本地模型没有返回完整 JSON。 ");
        var marker = candidate.IndexOf("\"courses\"", StringComparison.Ordinal);
        var arrayStart = marker < 0 ? -1 : candidate.IndexOf('[', marker);
        if (arrayStart < 0) throw new InvalidDataException("本地模型没有返回课程数组。 ");

        var courses = new List<AiScheduleCourse>();
        var objectStart = -1;
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = arrayStart + 1; index < candidate.Length; index++)
        {
            var ch = candidate[index];
            if (inString)
            {
                if (escaped) escaped = false;
                else if (ch == '\\') escaped = true;
                else if (ch == '"') inString = false;
                continue;
            }
            if (ch == '"') { inString = true; continue; }
            if (ch == '{')
            {
                if (depth++ == 0) objectStart = index;
            }
            else if (ch == '}' && depth > 0 && --depth == 0 && objectStart >= 0)
            {
                try
                {
                    var course = JsonSerializer.Deserialize<AiScheduleCourse>(candidate[objectStart..(index + 1)], Options);
                    if (course is not null) courses.Add(course);
                }
                catch (JsonException) { /* Ignore only the malformed object. */ }
                objectStart = -1;
            }
        }
        if (courses.Count == 0) throw new InvalidDataException("本地模型没有返回完整课程对象。 ");
        return new AiScheduleDocument { SchemaVersion = 1, DocumentType = "weekly_schedule", Courses = courses };
    }
}
