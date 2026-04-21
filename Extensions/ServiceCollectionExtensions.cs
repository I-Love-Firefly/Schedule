using Microsoft.Extensions.DependencyInjection;
using Schedule2._0.Services;
using Schedule2._0.Services.Adapters;
using Schedule2._0.ViewModels;
using Schedule2._0.Views;

namespace Schedule2._0.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddScheduleCoreServices(this IServiceCollection services)
        {
            services.AddSingleton<DatabaseService>();
            services.AddSingleton<ParserService>();
            services.AddSingleton<ConfigService>();
            services.AddSingleton<AlarmService>();
            services.AddSingleton<ThemeService>();

            return services;
        }

        public static IServiceCollection AddSchoolAdapters(this IServiceCollection services)
        {
            services.AddSingleton<ISchoolAdapter, XmumAdapter>();
            services.AddSingleton<ISchoolAdapter, FriendSchoolAdapter>();
            services.AddSingleton<ISchoolAdapterProvider, SchoolAdapterProvider>();

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
            services.AddSingleton<LoginPage>();
            services.AddTransient<AddCoursePage>();
            services.AddSingleton<SettingsPage>();
            services.AddSingleton<AppShell>();

            return services;
        }
    }
}
