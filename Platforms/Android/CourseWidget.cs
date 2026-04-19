using Android.App;
using Android.Appwidget;
using Android.Content;
using Android.Widget;
using Microsoft.Maui.Storage;
using System.Text.RegularExpressions;

namespace Schedule2._0.Platforms.Android;

[BroadcastReceiver(Label = "XMUM 课表小组件", Exported = true, Name = "site.justindividual.schedule.CourseWidget")]
[IntentFilter(new[] { "android.appwidget.action.APPWIDGET_UPDATE", "com.xmu.SCHEDULE_UPDATE_ACTION" })]
[MetaData("android.appwidget.provider", Resource = "@xml/course_widget_info")]
public class CourseWidget : AppWidgetProvider
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        base.OnReceive(context, intent);

        if (intent?.Action == AppWidgetManager.ActionAppwidgetUpdate ||
            intent?.Action == "com.xmu.SCHEDULE_UPDATE_ACTION")
        {
            var manager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context!, Java.Lang.Class.FromType(typeof(CourseWidget)));
            var ids = manager.GetAppWidgetIds(componentName);
            OnUpdate(context, manager, ids);

            // 已去掉闹钟自动接力逻辑
        }
    }
    
    public override void OnUpdate(Context? context, AppWidgetManager? appWidgetManager, int[]? appWidgetIds)
    {
        if (context == null || appWidgetManager == null || appWidgetIds == null) return;

        var rawData = Preferences.Default.Get("raw_schedule_data", "");
        // 已去掉 next_alarm_time 的读取

        foreach (var widgetId in appWidgetIds)
        {
            var views = new RemoteViews(context.PackageName, Resource.Layout.widget_layout);

            if (string.IsNullOrEmpty(rawData))
            {
                views.SetTextViewText(Resource.Id.widget_course_name, "请先在App同步");
                views.SetTextViewText(Resource.Id.widget_location, "暂无课程数据");
                appWidgetManager.UpdateAppWidget(widgetId, views);
                continue;
            }

            try
            {
                var now = DateTime.Now;
                views.SetTextViewText(Resource.Id.widget_current_date, now.ToString("M月d日"));
                views.SetTextViewText(Resource.Id.widget_current_day, GetChineseDayOfWeek(now.DayOfWeek));

                var entries = rawData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
                var allCourses = entries
                    .Select(e => e.Split(new[] { "##" }, StringSplitOptions.None))
                    .Where(p => p.Length >= 3)
                    .Select(p => {
                        var (start, end) = GetCourseTimeRange(p[1]);
                        return new
                        {
                            Name = p[0].Trim(),
                            StartTime = start,
                            EndTime = end,
                            // 如果有地点就显示，没有就空着
                            Location = p.Length >= 3 ? p[2].Trim() : "Unknown"
                        };
                    }).ToList();

                var currentCourse = allCourses.FirstOrDefault(c => now >= c.StartTime && now < c.EndTime);
                var nextCourse = allCourses.Where(c => c.StartTime >= now).OrderBy(c => c.StartTime).FirstOrDefault();

                if (currentCourse != null)
                {
                    views.SetTextViewText(Resource.Id.widget_motto, "正在上课：");
                    views.SetTextViewText(Resource.Id.widget_course_name, currentCourse.Name);

                    // --- 核心修复点：使用 widget_location ID，并拼接地点 ---
                    string timeRange = $"{currentCourse.StartTime:HH:mm}-{currentCourse.EndTime:HH:mm}";
                    // 只有当 Location 字段不为空时，才显示 | 分隔符
                    string info = string.IsNullOrEmpty(currentCourse.Location) 
                                  ? timeRange 
                                  : $"{timeRange} | {currentCourse.Location}";

                    views.SetTextViewText(Resource.Id.widget_location, info);
                }
                else if (nextCourse != null)
                {
                    bool isToday = nextCourse.StartTime.Date == now.Date;
                    string prefix = isToday ? "下一节课：" : $"{GetChineseDayOfWeek(nextCourse.StartTime.DayOfWeek)}：";

                    views.SetTextViewText(Resource.Id.widget_motto, prefix);
                    views.SetTextViewText(Resource.Id.widget_course_name, nextCourse.Name);

                    // --- 核心修复点：对齐 ID，拼接地点 ---
                    string timeRange = $"{nextCourse.StartTime:HH:mm}-{nextCourse.EndTime:HH:mm}";
                    string info = string.IsNullOrEmpty(nextCourse.Location) 
                                        ? timeRange 
                                        : $"{timeRange} | {nextCourse.Location}";
                    views.SetTextViewText(Resource.Id.widget_location, info);
                }
                else
                {
                    views.SetTextViewText(Resource.Id.widget_motto, "今天没课啦");
                    views.SetTextViewText(Resource.Id.widget_course_name, "回去休息吧");
                    views.SetTextViewText(Resource.Id.widget_location, "Enjoy your time!");
                }
            }
            catch
            {
                views.SetTextViewText(Resource.Id.widget_course_name, "数据异常");
            }

            appWidgetManager.UpdateAppWidget(widgetId, views);
        }
    }

    private string GetChineseDayOfWeek(DayOfWeek day)
    {
        string[] cnDays = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
        return cnDays[(int)day];
    }

    private (DateTime Start, DateTime End) GetCourseTimeRange(string courseInfo)
    {
        string[] days = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
        string dayName = days.FirstOrDefault(d => courseInfo.Contains(d)) ?? "";
        if (string.IsNullOrEmpty(dayName)) return (DateTime.MinValue, DateTime.MinValue);

        var timePart = courseInfo.Split(new[] { dayName }, StringSplitOptions.None)[1].Trim();
        var times = timePart.Split('-');

        // 关键修复：兼容手动输入的 "." 和 ":"
        string startTimeStr = times[0].Trim().Replace(".", ":");
        string endTimeStr = times[1].Trim().Split('(')[0].Trim().Replace(".", ":");

        DayOfWeek targetDay = (DayOfWeek)Enum.Parse(typeof(DayOfWeek), dayName);
        DateTime startDay = DateTime.Today.AddDays(((int)targetDay - (int)DateTime.Today.DayOfWeek + 7) % 7);

        DateTime.TryParse($"{startDay:yyyy-MM-dd} {startTimeStr}", out DateTime startResult);
        DateTime.TryParse($"{startDay:yyyy-MM-dd} {endTimeStr}", out DateTime endResult);

        // 如果解析出的结束时间在现在之前，说明是下周的这节课
        if (endResult < DateTime.Now)
        {
            startResult = startResult.AddDays(7);
            endResult = endResult.AddDays(7);
        }
        return (startResult, endResult);
    }
}