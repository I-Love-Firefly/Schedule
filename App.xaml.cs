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