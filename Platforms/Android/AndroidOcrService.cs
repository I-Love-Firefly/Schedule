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
                var box = line.BoundingBox;
                if (box is null || string.IsNullOrWhiteSpace(line.Text)) continue;
                regions.Add(new OcrTextRegion(line.Text, box.Left, box.Top, box.Right, box.Bottom));
            }
        }

        return new OcrDocument
        {
            ImageWidth = bitmap.Width,
            ImageHeight = bitmap.Height,
            Regions = regions
        };
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
