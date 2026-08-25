#if ANDROID
using Android.Graphics;
using Xamarin.Google.MLKit.Vision.Common;
using Xamarin.Google.MLKit.Vision.Text;
using Xamarin.Google.MLKit.Vision.Text.Chinese;
using Schedule2._0.Services.ImageImport;
using GmsTask = global::Android.Gms.Tasks.Task;
using IOnCompleteListener = global::Android.Gms.Tasks.IOnCompleteListener;
using CancellationToken = System.Threading.CancellationToken;

namespace Schedule2._0.Platforms.Android;

public sealed class AndroidOcrService : Java.Lang.Object, IOcrService
{
    public bool IsSupported => true;

    public async Task<OcrDocument> RecognizeAsync(string imagePath, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var bitmap = await DecodeBitmapAsync(imagePath, cancellationToken);
        using var image = InputImage.FromBitmap(bitmap, 0);
        using var options = new ChineseTextRecognizerOptions.Builder().Build();
        using var recognizer = TextRecognition.GetClient(options);
        using var result = await AwaitTextAsync(recognizer.Process(image), cancellationToken);

        var regions = new List<OcrTextRegion>();
        foreach (var block in result.TextBlocks)
        {
            foreach (var line in block.Lines)
            {
                if (line.Elements.Count > 0)
                {
                    foreach (var element in line.Elements)
                    {
                        var elementBox = element.BoundingBox;
                        if (elementBox is null || string.IsNullOrWhiteSpace(element.Text)) continue;
                        regions.Add(new OcrTextRegion(
                            element.Text,
                            elementBox.Left,
                            elementBox.Top,
                            elementBox.Right,
                            elementBox.Bottom));
                    }
                    continue;
                }

                var lineBox = line.BoundingBox;
                if (lineBox is null || string.IsNullOrWhiteSpace(line.Text)) continue;
                regions.Add(new OcrTextRegion(line.Text, lineBox.Left, lineBox.Top, lineBox.Right, lineBox.Bottom));
            }
        }

        var pixels = new int[bitmap.Width * bitmap.Height];
        bitmap.GetPixels(pixels, 0, bitmap.Width, 0, 0, bitmap.Width, bitmap.Height);

        return new OcrDocument
        {
            ImageWidth = bitmap.Width,
            ImageHeight = bitmap.Height,
            Regions = regions,
            HorizontalLines = DetectGridLines(pixels, bitmap.Width, bitmap.Height, horizontal: true),
            VerticalLines = DetectGridLines(pixels, bitmap.Width, bitmap.Height, horizontal: false)
        };
    }

    private static IReadOnlyList<float> DetectGridLines(int[] pixels, int width, int height, bool horizontal)
    {
        var length = horizontal ? height : width;
        var crossLength = horizontal ? width : height;
        var counts = new int[length];

        for (var y = 0; y < height; y++)
        for (var x = 0; x < width; x++)
        {
            var color = pixels[y * width + x];
            var red = (color >> 16) & 0xff;
            var green = (color >> 8) & 0xff;
            var blue = color & 0xff;
            if (red < 145 && green < 145 && blue < 145)
                counts[horizontal ? y : x]++;
        }

        var threshold = (int)(crossLength * (horizontal ? 0.28 : 0.35));
        var candidates = Enumerable.Range(0, length).Where(i => counts[i] >= threshold).ToList();
        if (candidates.Count == 0) return [];

        var merged = new List<float>();
        var group = new List<int> { candidates[0] };
        for (var i = 1; i < candidates.Count; i++)
        {
            if (candidates[i] - group[^1] <= 3)
                group.Add(candidates[i]);
            else
            {
                merged.Add((float)group.Average());
                group = [candidates[i]];
            }
        }
        merged.Add((float)group.Average());
        return merged;
    }

    private static Task<Bitmap> DecodeBitmapAsync(string path, CancellationToken cancellationToken) =>
        Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();
            var bounds = new BitmapFactory.Options { InJustDecodeBounds = true };
            BitmapFactory.DecodeFile(path, bounds);
            var maxSide = Math.Max(bounds.OutWidth, bounds.OutHeight);
            var sampleSize = 1;
            while (maxSide / sampleSize > 2600) sampleSize *= 2;

            var options = new BitmapFactory.Options { InSampleSize = sampleSize };
            return BitmapFactory.DecodeFile(path, options)
                   ?? throw new InvalidDataException("无法读取所选图片，请改用 PNG 或 JPEG 格式。");
        }, cancellationToken);

    private static async Task<Text> AwaitTextAsync(GmsTask task, CancellationToken cancellationToken)
    {
        var source = new TaskCompletionSource<Text>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(() => source.TrySetCanceled(cancellationToken));
        task.AddOnCompleteListener(new TextTaskListener(source));
        return await source.Task;
    }

    private sealed class TextTaskListener(TaskCompletionSource<Text> source) : Java.Lang.Object, IOnCompleteListener
    {
        public void OnComplete(GmsTask task)
        {
            if (task.IsSuccessful && task.Result is Text text)
                source.TrySetResult(text);
            else
                source.TrySetException(new InvalidOperationException(task.Exception?.LocalizedMessage ?? "离线 OCR 处理失败。"));
        }
    }
}
#endif
