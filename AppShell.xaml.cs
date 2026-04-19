using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Maui.Controls;
using Schedule2._0.Services;
using Schedule2._0.Views;
using Schedule2._0.Models;

namespace Schedule2._0
{
    public partial class AppShell : Shell
    {
        private readonly ConfigService _config;

        public AppShell(ConfigService configService)
        {
            InitializeComponent();
            _config = configService;

            WeakReferenceMessenger.Default.Register<ThemeChangedMessage>(this, (r, m) =>
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    // 收到主题切换信号，立刻重刷状态栏
                    Schedule2._0.Helpers.ThemeHelper.SyncStatusBar(this, _config);
                });
            });

            Routing.RegisterRoute(nameof(Views.LoginPage), typeof(Views.LoginPage));
            Routing.RegisterRoute(nameof(AddCoursePage), typeof(AddCoursePage));
            Routing.RegisterRoute(nameof(SettingsPage), typeof(SettingsPage));
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // 给系统 100 毫秒的时间完成 UI 挂载
            await Task.Delay(100);

            // 这时候再染色，成功率就是 100% 了
            Schedule2._0.Helpers.ThemeHelper.SyncStatusBar(this, _config);
        }
    }
}
