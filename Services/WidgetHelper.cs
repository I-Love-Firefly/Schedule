#if ANDROID
using Android.Appwidget;
using Android.Content;
#endif

namespace Schedule2._0.Services
{
    public static class WidgetHelper
    {
        public static void ForceRefreshWidget()
        {
#if ANDROID
            var context = Android.App.Application.Context;
            var intent = new Intent(context, typeof(Schedule2._0.Platforms.Android.CourseWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            var widgetManager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Schedule2._0.Platforms.Android.CourseWidget)));
            var ids = widgetManager.GetAppWidgetIds(componentName);
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);
            context.SendBroadcast(intent);
#endif
        }
    }
}
