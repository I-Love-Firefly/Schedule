namespace Schedule2._0.Services.ImageImport;

public interface ICloudScheduleAiService
{
    bool IsEnabled { get; }
    Task<CloudScheduleAiResponse> RecognizeAsync(
        string imagePath,
        OcrDocument document,
        CancellationToken cancellationToken = default);
}

public sealed record CloudScheduleAiResponse(
    string Content,
    string Provider,
    string Model,
    long ElapsedMs,
    string RequestId);
