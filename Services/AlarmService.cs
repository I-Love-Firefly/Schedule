#if ANDROID
using Android.App;
using Android.Content;
using Android.Appwidget;
#endif
using Microsoft.Maui.Storage;

namespace Schedule2._0.Services
{
    public class AlarmService
    {
        private readonly DatabaseService _dbService;

        public AlarmService(DatabaseService dbService)
        {
            _dbService = dbService;
        }

        public async Task RescheduleNextAlarm()
        {
#if ANDROID
    var context = Android.App.Application.Context;
#endif
            var allCourses = await _dbService.GetCoursesAsync();
            if (allCourses == null || !allCourses.Any()) return;

            var now = DateTime.Now;
            DateTime? nextAlarmTime = null;
            string alarmReason = "";

            for (int i = 0; i < 7; i++)
            {
                var testDate = DateTime.Today.AddDays(i);
                foreach (var course in allCourses)
                {
                    // --- 关键解析修复逻辑 ---
                    if (!TryParseFlexibleTime(course.StartTime, out TimeSpan startTS)) continue;
                    if (!TryParseFlexibleTime(course.EndTime, out TimeSpan endTS)) continue;

                    DayOfWeek courseDay = GetDayOfWeekFromString(course.DayOfWeek);

                    if (testDate.DayOfWeek == courseDay)
                    {
                        var startDateTime = testDate.Add(startTS);
                        var endDateTime = testDate.Add(endTS);

                        if (startDateTime > now && (nextAlarmTime == null || startDateTime < nextAlarmTime))
                        {
                            nextAlarmTime = startDateTime;
                            alarmReason = (i == 0) ? "上课" : $"{GetChineseDay(courseDay)}上课";
                        }
                        if (endDateTime > now && (nextAlarmTime == null || endDateTime < nextAlarmTime))
                        {
                            nextAlarmTime = endDateTime;
                            alarmReason = (i == 0) ? "下课" : $"{GetChineseDay(courseDay)}下课";
                        }
                    }
                }
                if (nextAlarmTime.HasValue) break;
            }

            if (nextAlarmTime.HasValue)
            {
                string displayStr = $"{nextAlarmTime.Value:HH:mm} ({alarmReason})";
                Microsoft.Maui.Storage.Preferences.Default.Set("next_alarm_time", displayStr);
                SetNativeAlarm(nextAlarmTime.Value, alarmReason);
            }
            else
            {
                Microsoft.Maui.Storage.Preferences.Default.Set("next_alarm_time", "近期无课程");
            }
        }

        // 辅助方法：处理 10.00am, 1:00pm 等各种奇葩格式
        private bool TryParseFlexibleTime(string? timeStr, out TimeSpan result)
        {
            result = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(timeStr)) return false;

            // 1. 统一格式：把点换成冒号，去掉空格
            string cleanTime = timeStr.Replace(".", ":").Replace(" ", "").ToLower();

            // 2. 尝试用 DateTime 解析（它支持 AM/PM），然后转为 TimeSpan
            if (DateTime.TryParse(cleanTime, out DateTime dt))
            {
                result = dt.TimeOfDay;
                return true;
            }

            // 3. 备用方案：传统的 TimeSpan 解析
            return TimeSpan.TryParse(cleanTime, out result);
        }

        // 这个方法负责把各种格式的星期字符串（包含大小写、空格、缩写）统一转换
        private DayOfWeek GetDayOfWeekFromString(string? dayStr)
        {
            if (string.IsNullOrWhiteSpace(dayStr)) return DayOfWeek.Sunday;

            // 清理字符串：转小写并去掉首尾空格
            dayStr = dayStr.Trim().ToLower();

            // 匹配常见的各种写法
            if (dayStr.Contains("mon") || dayStr == "1") return DayOfWeek.Monday;
            if (dayStr.Contains("tue") || dayStr == "2") return DayOfWeek.Tuesday;
            if (dayStr.Contains("wed") || dayStr == "3") return DayOfWeek.Wednesday;
            if (dayStr.Contains("thu") || dayStr == "4") return DayOfWeek.Thursday;
            if (dayStr.Contains("fri") || dayStr == "5") return DayOfWeek.Friday;
            if (dayStr.Contains("sat") || dayStr == "6") return DayOfWeek.Saturday;
            if (dayStr.Contains("sun") || dayStr == "7" || dayStr == "0") return DayOfWeek.Sunday;

            // 如果上面的模糊匹配都没中，尝试标准转换
            if (Enum.TryParse<DayOfWeek>(dayStr, true, out var result))
            {
                return result;
            }

            return DayOfWeek.Sunday; // 默认兜底
        }

        // 必须添加这个方法，放在 RescheduleNextAlarm 下面，类的大括号里面
        private string GetChineseDay(DayOfWeek day)
        {
            string[] cnDays = { "周日", "周一", "周二", "周三", "周四", "周五", "周六" };
            return cnDays[(int)day];
        }

        private void SetNativeAlarm(DateTime target, string reason)
        {
#if ANDROID
            var context = Android.App.Application.Context;
            
            // 获取小组件 ID 列表
            var appWidgetManager = AppWidgetManager.GetInstance(context);
            var componentName = new ComponentName(context, Java.Lang.Class.FromType(typeof(Platforms.Android.CourseWidget)));
            var ids = appWidgetManager.GetAppWidgetIds(componentName);

            var intent = new Intent(context, typeof(Platforms.Android.CourseWidget));
            intent.SetAction(AppWidgetManager.ActionAppwidgetUpdate);
            // 必须放入 IDs，否则 OnUpdate 不会触发
            intent.PutExtra(AppWidgetManager.ExtraAppwidgetIds, ids);

            var flags = PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable;
            var pendingIntent = PendingIntent.GetBroadcast(context, 0, intent, flags);

            var alarmManager = (AlarmManager)context.GetSystemService(Context.AlarmService);
            long triggerAtMs = new DateTimeOffset(target).ToUnixTimeMilliseconds();

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.M)
                alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerAtMs, pendingIntent);
            else
                alarmManager.Set(AlarmType.RtcWakeup, triggerAtMs, pendingIntent);

            //这行代码只是为了调试用
            //Android.Widget.Toast.MakeText(context, $"[Alarm] 预约{reason}: {target:HH:mm}", Android.Widget.ToastLength.Short).Show();
#endif
        }
    }
}