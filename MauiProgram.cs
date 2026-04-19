using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Schedule2._0.Services;
using Schedule2._0.ViewModels;
using Schedule2._0.Views;
using Schedule2._0.Services.Adapters;



#if ANDROID
using Android.Webkit;
#endif

namespace Schedule2._0
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("MaterialSymbols.ttf", "MaterialSymbols");
                });

#if DEBUG
            builder.Logging.AddDebug();
#endif
            // 1. 注册 Services
            builder.Services.AddSingleton<Services.DatabaseService>();
            builder.Services.AddSingleton<Services.ParserService>();
            builder.Services.AddSingleton<Services.ConfigService>();
            builder.Services.AddSingleton<Services.AlarmService>();
            builder.Services.AddSingleton<ThemeService>();

            // ========================================
            // 学校适配器注册
            // ========================================
            // 【说明】课表导入功能需要一个学校适配器来处理特定学校的教务系统
            // 【步骤】
            // 1. 参考 Services/Adapters/SchoolAdapterTemplate.cs 创建新适配器
            // 2. 在下面注册您的适配器 (同一时间只能注册一个)
            // 3. 编译运行并测试导入功能
            //
            // 【示例】
            // builder.Services.AddSingleton<ISchoolAdapter, XmumAdapter>();        // XMUM学校
            // builder.Services.AddSingleton<ISchoolAdapter, StanfordAdapter>();    // Stanford学校
            // builder.Services.AddSingleton<ISchoolAdapter, YourSchoolAdapter>();  // 您的学校
            builder.Services.AddSingleton<ISchoolAdapter, XmumAdapter>();
            // ========================================

            builder.Services.AddSingleton<AppShell>();

            // 2. 注册 ViewModels
            builder.Services.AddSingleton<ViewModels.MainViewModel>();
            builder.Services.AddTransient<ViewModels.AddCourseViewModel>();
            builder.Services.AddSingleton<ViewModels.SettingsViewModel>();

            // 3. 注册 Views
            builder.Services.AddTransient<Views.MainPage>();
            builder.Services.AddSingleton<Views.LoginPage>();
            builder.Services.AddTransient<Views.AddCoursePage>();
            builder.Services.AddSingleton<Views.SettingsPage>();

            // --- [新增部分：1.0 版本显示适配逻辑移植] ---
#if ANDROID
            WebViewHandler.Mapper.AppendToMapping("MyCustomWebView", (handler, view) =>
            {
                // 设置安卓原生 WebView 属性，使其自动缩放并支持宽屏显示
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.UseWideViewPort = true;      // 关键：对应 1.0 的宽视口适配
                handler.PlatformView.Settings.LoadWithOverviewMode = true; // 关键：对应 1.0 的全景加载模式
                handler.PlatformView.Settings.BuiltInZoomControls = true;  // 启用多点触控缩放
                handler.PlatformView.Settings.DisplayZoomControls = false; // 隐藏自带的丑丑的缩放按钮
                handler.PlatformView.Settings.DomStorageEnabled = true;    // 确保教务系统登录状态正常
            });
#endif
            // ------------------------------------------

            return builder.Build();
        }
    }
}