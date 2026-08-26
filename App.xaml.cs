using CommunityToolkit.Mvvm.Messaging;
using Schedule2._0.Helpers;
using Schedule2._0.Models;
using Schedule2._0.Services;

namespace Schedule2._0
{
    public partial class App : Application
    {
        private readonly DatabaseService _dbService;
        private readonly ThemeService _themeService;
        private readonly ConfigService _configService;

        public App(DatabaseService dbService, ThemeService themeService, ConfigService configService, AppShell shell)
        {
            InitializeComponent();

            _dbService = dbService;
            _themeService = themeService;
            _configService = configService;

            RemoveObsoleteLocalScheduleModel();

            ApplySystemTheme(_configService.AppTheme);

            MainPage = shell;

            // --- 核心改动：监听系统主题实时变化 ---
            RequestedThemeChanged += (s, a) =>
            {
                // 只有在用户选择了“跟随系统”(0)时才响应
                if (_configService.AppTheme == 0)
                {
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        ApplySystemTheme(0);
                    });
                }
            };
        }

        private static void RemoveObsoleteLocalScheduleModel()
        {
            var modelDirectory = Path.Combine(FileSystem.AppDataDirectory, "models");
            var modelPath = Path.Combine(modelDirectory, "MiniCPM5-1B-Schedule-Q4_K_M.gguf");
            try
            {
                if (File.Exists(modelPath)) File.Delete(modelPath);
                if (File.Exists(modelPath + ".partial")) File.Delete(modelPath + ".partial");
                if (Directory.Exists(modelDirectory) && !Directory.EnumerateFileSystemEntries(modelDirectory).Any())
                    Directory.Delete(modelDirectory);
            }
            catch (IOException)
            {
                // Cleanup is best effort and must never block application startup.
            }
            catch (UnauthorizedAccessException)
            {
                // The obsolete model remains unused even when the OS refuses deletion.
            }
        }

        protected override void OnStart()
        {
            base.OnStart();

            // 1. 获取各项数据
            int savedMode = _configService.AppTheme;
            var systemTheme = RequestedTheme; // 系统当前的请求
            var appTheme = UserAppTheme;      // 框架当前设置的状态

            // 2. 执行应用逻辑
            ApplySystemTheme(savedMode);
        }

        public void ApplySystemTheme(int mode)
        {
            // 1. 设置框架主题
            UserAppTheme = mode switch { 1 => AppTheme.Light, 2 => AppTheme.Dark, _ => AppTheme.Unspecified };

            // 2. 刷新资源
            _themeService.ApplyTheme(mode);

            // 3. 【关键新增】：发送广播信号
            WeakReferenceMessenger.Default.Send(new ThemeChangedMessage("Update"));

            // 顺便手动刷一下当前的 MainPage
            if (MainPage != null)
            {
                ThemeHelper.SyncStatusBar(MainPage, _configService);
            }
        }
    }
}
