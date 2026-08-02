using Microsoft.Extensions.DependencyInjection;
using Schedule2._0.Services;
using Schedule2._0.Services.ImageImport;
using Schedule2._0.ViewModels;
using Schedule2._0.Views;

namespace Schedule2._0.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddScheduleCoreServices(this IServiceCollection services)
        {
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<AlarmService>();
            services.AddSingleton<ThemeService>();
            services.AddSingleton<ScheduleImageParser>();
#if ANDROID
            services.AddSingleton<IOcrService, Schedule2._0.Platforms.Android.AndroidOcrService>();
#else
            services.AddSingleton<IOcrService, UnsupportedOcrService>();
#endif

            return services;
        }

        public static IServiceCollection AddScheduleViewModels(this IServiceCollection services)
        {
            services.AddSingleton<MainViewModel>();
            services.AddTransient<AddCourseViewModel>();
            services.AddSingleton<SettingsViewModel>();

            return services;
        }

        public static IServiceCollection AddScheduleViews(this IServiceCollection services)
        {
            services.AddTransient<MainPage>();
            services.AddTransient<AddCoursePage>();
            services.AddTransient<ScheduleImageImportPage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<AppShell>();

            return services;
        }
    }
}
