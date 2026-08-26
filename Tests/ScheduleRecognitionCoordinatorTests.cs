using Schedule2._0.Services.ImageImport;
using Xunit;

namespace Schedule2._0.Tests;

public sealed class ScheduleRecognitionCoordinatorTests
{
    [Fact]
    public async Task UsesApiResult()
    {
        var document = BasicDocument();
        const string output = "{\"schemaVersion\":1,\"documentType\":\"weekly_schedule\",\"courses\":[" +
                              "{\"name\":\"高等数学\",\"teacher\":\"张老师\",\"location\":\"A101\",\"dayOfWeek\":\"Monday\"," +
                              "\"startPeriod\":1,\"endPeriod\":2,\"startTime\":\"08:00\",\"endTime\":\"09:40\",\"weeks\":[]}]}";
        var parser = new ScheduleImageParser();
        var coordinator = new ScheduleRecognitionCoordinator(
            parser,
            new FakeCloudService(output));

        var result = await coordinator.RecognizeAsync("unused.png", document, TestContext.Current.CancellationToken);

        Assert.Equal("高等数学", Assert.Single(result.Courses).Name);
        Assert.Contains("DeepSeek", result.RecognitionSource);
        Assert.DoesNotContain(result.Warnings, x => x.Contains("本地识别", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PropagatesApiFailureWithoutLocalFallback()
    {
        var document = BasicDocument();
        var parser = new ScheduleImageParser();
        var coordinator = new ScheduleRecognitionCoordinator(
            parser,
            new ThrowingCloudService());

        await Assert.ThrowsAsync<HttpRequestException>(() =>
            coordinator.RecognizeAsync("unused.png", document, TestContext.Current.CancellationToken));
    }

    private static OcrDocument BasicDocument() => new()
    {
        ImageWidth = 1000,
        ImageHeight = 800,
        Regions =
        [
            new OcrTextRegion("星期一", 180, 30, 250, 60),
            new OcrTextRegion("星期二", 360, 30, 430, 60),
            new OcrTextRegion("08:00-09:40", 10, 100, 140, 130),
            new OcrTextRegion("高等数学", 170, 100, 300, 130),
            new OcrTextRegion("张老师 A101", 170, 135, 300, 165)
        ]
    };

    private sealed class FakeCloudService(string output) : ICloudScheduleAiService
    {
        public bool IsEnabled => true;
        public Task<CloudScheduleAiResponse> RecognizeAsync(string imagePath, OcrDocument document,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new CloudScheduleAiResponse(output, "DeepSeek 官方 API", "DeepSeek test", 1234, "test"));
    }

    private sealed class ThrowingCloudService : ICloudScheduleAiService
    {
        public bool IsEnabled => true;
        public Task<CloudScheduleAiResponse> RecognizeAsync(string imagePath, OcrDocument document,
            CancellationToken cancellationToken = default) => throw new HttpRequestException("offline");
    }
}
