using System.Net.Http.Json;
using System.Text.Json;
using Schedule2._0.Services;

namespace Schedule2._0.Services.ImageImport;

public sealed class CloudScheduleAiService(ConfigService configService, HttpClient httpClient) : ICloudScheduleAiService
{
    private const int MaximumImageBytes = 10 * 1024 * 1024;

    public bool IsEnabled =>
        Uri.TryCreate(configService.CloudRecognitionEndpoint, UriKind.Absolute, out var uri) &&
        uri.Scheme == Uri.UriSchemeHttps;

    public async Task<CloudScheduleAiResponse> RecognizeAsync(
        string imagePath,
        OcrDocument document,
        CancellationToken cancellationToken = default)
    {
        if (!IsEnabled) throw new InvalidOperationException("课程表 API 服务地址无效。");

        var image = await File.ReadAllBytesAsync(imagePath, cancellationToken);
        if (image.Length == 0 || image.Length > MaximumImageBytes)
            throw new InvalidDataException("课程表图片必须小于 10 MB。");

        var extension = Path.GetExtension(imagePath).ToLowerInvariant();
        var mimeType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".webp" => "image/webp",
            ".png" => "image/png",
            _ => throw new InvalidDataException("云端 DeepSeek 仅支持 JPEG、PNG、GIF 或 WebP 图片。")
        };
        var request = new
        {
            imageBase64 = Convert.ToBase64String(image),
            mimeType,
            ocrLayout = OcrLayoutSerializer.Serialize(document)
        };

        using var response = await httpClient.PostAsJsonAsync(
            $"{configService.CloudRecognitionEndpoint}/v1/schedules/recognize",
            request,
            cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"云端识别服务返回 {(int)response.StatusCode}：{ReadError(body)}");

        var payload = JsonSerializer.Deserialize<GatewayResponse>(body, JsonOptions)
                      ?? throw new InvalidDataException("云端识别服务返回空响应。");
        if (string.IsNullOrWhiteSpace(payload.Content))
            throw new InvalidDataException("云端模型没有返回课程数据。");
        return new CloudScheduleAiResponse(
            payload.Content,
            payload.Provider ?? "cloud",
            payload.Model ?? "unknown",
            payload.ElapsedMs,
            payload.RequestId ?? "");
    }

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private static string ReadError(string body)
    {
        try
        {
            return JsonSerializer.Deserialize<GatewayError>(body, JsonOptions)?.Error ?? "请求失败";
        }
        catch (JsonException) { return "请求失败"; }
    }

    private sealed class GatewayResponse
    {
        public string? Content { get; set; }
        public string? Provider { get; set; }
        public string? Model { get; set; }
        public long ElapsedMs { get; set; }
        public string? RequestId { get; set; }
    }

    private sealed class GatewayError { public string? Error { get; set; } }
}
