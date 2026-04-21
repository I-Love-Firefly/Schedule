using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Handlers;
using Schedule2._0.Extensions;



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

            builder.Services
                .AddScheduleCoreServices()
                .AddSchoolAdapters()
                .AddScheduleViewModels()
                .AddScheduleViews();

#if ANDROID
            WebViewHandler.Mapper.AppendToMapping("MyCustomWebView", (handler, view) =>
            {
                handler.PlatformView.Settings.JavaScriptEnabled = true;
                handler.PlatformView.Settings.UseWideViewPort = true;
                handler.PlatformView.Settings.LoadWithOverviewMode = true;
                handler.PlatformView.Settings.BuiltInZoomControls = true;
                handler.PlatformView.Settings.DisplayZoomControls = false;
                handler.PlatformView.Settings.DomStorageEnabled = true;
            });
#endif

            return builder.Build();
        }
    }
}