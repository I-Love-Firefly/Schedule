#if ANDROID
using System.Runtime.InteropServices;
using System.Text;
using Schedule2._0.Services.ImageImport;

namespace Schedule2._0.Platforms.Android;

public sealed class AndroidScheduleAiService : IScheduleAiService
{
    private const string NativeLibrary = "schedule_ai";
    private const string FileName = "MiniCPM5-1B-Schedule-Q4_K_M.gguf";
    private static readonly SemaphoreSlim InferenceLock = new(1, 1);
    private readonly string _modelPath = Path.Combine(FileSystem.AppDataDirectory, "models", FileName);

    public bool IsSupported => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
    public bool IsModelInstalled => File.Exists(_modelPath) && new FileInfo(_modelPath).Length > 100_000_000;
    public string ModelFileName => FileName;

    public async Task InstallModelAsync(Stream source, CancellationToken cancellationToken = default)
    {
        if (!IsSupported) throw new PlatformNotSupportedException("本地 AI 需要 arm64 Android 设备。 ");
        Directory.CreateDirectory(Path.GetDirectoryName(_modelPath)!);
        var temporary = _modelPath + ".partial";
        try
        {
            await using (var output = new FileStream(temporary, FileMode.Create, FileAccess.Write, FileShare.None, 1024 * 1024, true))
                await source.CopyToAsync(output, 1024 * 1024, cancellationToken);
            if (new FileInfo(temporary).Length < 100_000_000)
                throw new InvalidDataException("所选 GGUF 模型文件过小。 ");
            File.Move(temporary, _modelPath, true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public async Task<string> StructureAsync(OcrDocument document, CancellationToken cancellationToken = default)
    {
        if (!IsModelInstalled) throw new FileNotFoundException("请先安装课程表 AI 模型。", _modelPath);
        await InferenceLock.WaitAsync(cancellationToken);
        try
        {
            return await Task.Run(() => Generate(OcrLayoutSerializer.ToChatPrompt(document), cancellationToken), cancellationToken);
        }
        finally
        {
            InferenceLock.Release();
        }
    }

    private string Generate(string prompt, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        NativeMethods.ScheduleAiBackendInit();
        var handle = NativeMethods.ScheduleAiCreate(_modelPath, 6144, Math.Clamp(Environment.ProcessorCount - 2, 2, 8));
        if (handle == IntPtr.Zero) throw new InvalidOperationException(ReadError());
        try
        {
            var capacity = 512 * 1024;
            var output = Marshal.AllocHGlobal(capacity);
            try
            {
                var written = NativeMethods.ScheduleAiGenerate(handle, prompt, 2048, output, capacity);
                if (written < 0) throw new InvalidOperationException(ReadError());
                var bytes = new byte[written];
                Marshal.Copy(output, bytes, 0, written);
                return Encoding.UTF8.GetString(bytes);
            }
            finally { Marshal.FreeHGlobal(output); }
        }
        finally
        {
            NativeMethods.ScheduleAiDestroy(handle);
        }
    }

    private static string ReadError()
    {
        var pointer = NativeMethods.ScheduleAiLastError();
        return pointer == IntPtr.Zero ? "本地 AI 推理失败。" : Marshal.PtrToStringUTF8(pointer) ?? "本地 AI 推理失败。";
    }

    private static class NativeMethods
    {
        [DllImport(NativeLibrary, EntryPoint = "schedule_ai_backend_init")]
        internal static extern void ScheduleAiBackendInit();

        [DllImport(NativeLibrary, EntryPoint = "schedule_ai_create", CharSet = CharSet.Ansi)]
        internal static extern IntPtr ScheduleAiCreate([MarshalAs(UnmanagedType.LPUTF8Str)] string modelPath, int contextSize, int threads);

        [DllImport(NativeLibrary, EntryPoint = "schedule_ai_generate", CharSet = CharSet.Ansi)]
        internal static extern int ScheduleAiGenerate(IntPtr handle, [MarshalAs(UnmanagedType.LPUTF8Str)] string prompt, int maxTokens, IntPtr output, int outputCapacity);

        [DllImport(NativeLibrary, EntryPoint = "schedule_ai_destroy")]
        internal static extern void ScheduleAiDestroy(IntPtr handle);

        [DllImport(NativeLibrary, EntryPoint = "schedule_ai_last_error")]
        internal static extern IntPtr ScheduleAiLastError();
    }
}
#endif
