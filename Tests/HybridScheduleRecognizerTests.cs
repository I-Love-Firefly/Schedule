using Schedule2._0.Services.ImageImport;
using Xunit;

namespace Schedule2._0.Tests;

public sealed class HybridScheduleRecognizerTests
{
    [Fact]
    public void LayoutSerializer_NormalizesCoordinatesAndReadingOrder()
    {
        var document = new OcrDocument
        {
            ImageWidth = 200,
            ImageHeight = 100,
            Regions =
            [
                new OcrTextRegion("高等数学", 100, 50, 180, 70),
                new OcrTextRegion("星期一", 20, 10, 60, 20)
            ]
        };

        var prompt = OcrLayoutSerializer.Serialize(document);

        Assert.Contains("[100,100,300,200] 星期一", prompt);
        Assert.Contains("[500,500,900,700] 高等数学", prompt);
        Assert.True(prompt.IndexOf("星期一", StringComparison.Ordinal) < prompt.IndexOf("高等数学", StringComparison.Ordinal));
    }

    [Fact]
    public void JsonParser_StripsThinkingAndCodeFence()
    {
        const string output = "<think>ignored</think>```json\n{\"schemaVersion\":1,\"documentType\":\"weekly_schedule\",\"courses\":[]}\n```";
        var parsed = AiScheduleJsonParser.Parse(output);
        Assert.Equal("weekly_schedule", parsed.DocumentType);
        Assert.Empty(parsed.Courses);
    }

    [Fact]
    public void JsonParser_RecoversCompleteObjectsFromTruncatedRepetition()
    {
        const string output = "{\"schemaVersion\":1,\"documentType\":\"weekly_schedule\",\"courses\":[" +
            "{\"name\":\"高等数学\",\"teacher\":\"张老师\",\"location\":\"A101\",\"dayOfWeek\":\"Monday\"," +
            "\"startPeriod\":1,\"endPeriod\":2,\"startTime\":\"08:00\",\"endTime\":\"09:40\",\"weeks\":\"1-16周\"}," +
            "{\"name\":\"不完整";
        var parsed = AiScheduleJsonParser.Parse(output);
        var course = Assert.Single(parsed.Courses);
        Assert.Equal("高等数学", course.Name);
    }

    [Fact]
    public async Task HybridRecognizer_RejectsHallucinatedCourseBeforeWrite()
    {
        var document = BasicDocument();
        var json = "{\"schemaVersion\":1,\"documentType\":\"weekly_schedule\",\"courses\":[" +
                   "{\"name\":\"量子魔法\",\"teacher\":\"\",\"location\":\"\",\"dayOfWeek\":\"Monday\"," +
                   "\"startPeriod\":1,\"endPeriod\":2,\"startTime\":\"08:00\",\"endTime\":\"09:40\",\"weeks\":\"1-16周\"}]}";
        var recognizer = new HybridScheduleRecognizer(new ScheduleImageParser(), new FakeAiService(json));

        var result = await recognizer.RecognizeAsync(document);

        var course = Assert.Single(result.Courses);
        Assert.Equal("高等数学", course.Name);
        Assert.DoesNotContain(result.Courses, x => x.Name == "量子魔法");
        Assert.Contains(result.Warnings, x => x.Contains("未能可靠复核", StringComparison.Ordinal));
    }

    private static OcrDocument BasicDocument() => new()
    {
        ImageWidth = 1000,
        ImageHeight = 800,
        Regions =
        [
            new OcrTextRegion("星期一", 180, 30, 250, 60),
            new OcrTextRegion("星期二", 360, 30, 430, 60),
            new OcrTextRegion("08:00-08:45", 10, 100, 140, 130),
            new OcrTextRegion("高等数学", 170, 100, 300, 130)
        ]
    };

    private sealed class FakeAiService(string output) : IScheduleAiService
    {
        public bool IsSupported => true;
        public bool IsModelInstalled => true;
        public string ModelFileName => "test.gguf";
        public Task InstallModelAsync(Stream source, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<string> StructureAsync(OcrDocument document, CancellationToken cancellationToken = default) =>
            Task.FromResult(output);
    }
}
