namespace Schedule2._0.Services.ImageImport;

public sealed record OcrTextRegion(
    string Text,
    float Left,
    float Top,
    float Right,
    float Bottom)
{
    public float CenterX => (Left + Right) / 2f;
    public float CenterY => (Top + Bottom) / 2f;
    public float Width => Right - Left;
    public float Height => Bottom - Top;
}

public sealed class OcrDocument
{
    public int ImageWidth { get; init; }
    public int ImageHeight { get; init; }
    public IReadOnlyList<OcrTextRegion> Regions { get; init; } = [];
    public IReadOnlyList<float> HorizontalLines { get; init; } = [];
    public IReadOnlyList<float> VerticalLines { get; init; } = [];
}

public interface IOcrService
{
    bool IsSupported { get; }
    Task<OcrDocument> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default);
}

public sealed class UnsupportedOcrService : IOcrService
{
    public bool IsSupported => false;

    public Task<OcrDocument> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default) =>
        throw new PlatformNotSupportedException("课程表截图识别目前仅支持 Android。");
}
