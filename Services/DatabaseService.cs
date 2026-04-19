using SQLite;
using Schedule2._0.Models;
using System.Globalization;
using System.Text.RegularExpressions;
#if ANDROID
using Android.Appwidget;
using Android.Content;
#endif

namespace Schedule2._0.Services
{
    public class DatabaseService
    {
        // 1. 移除字段 _serviceProvider
        private SQLiteAsyncConnection? _db;
        private readonly List<string> _dayOrder = new() { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };

        // 2. 构造函数变回最简单的样子，不请求任何参数
        public DatabaseService()
        {
        }

        async Task Init()
        {
            if (_db != null) return;
            var path = Path.Combine(FileSystem.AppDataDirectory, "schedule.db3");
            _db = new SQLiteAsyncConnection(path);
            await _db.CreateTableAsync<Course>();
        }

        public async Task NotifyWidgetAsync()
        {
            await Init();
            var allCourses = await _db.Table<Course>().ToListAsync();
            var rawStrings = allCourses.Select(c => $"{c.Name}##{c.DayOfWeek} {c.StartTime}-{c.EndTime}##{c.Location}");
            string finalData = string.Join("||", rawStrings);
            Preferences.Default.Set("raw_schedule_data", finalData);

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

            // 3. 核心修正：使用 IPlatformApplication 异步获取服务，避开死锁
            try
            {
                // 通过当前运行的 Handler 动态获取服务，而不是在构造函数里写死
                var alarmService = IPlatformApplication.Current.Services.GetService<AlarmService>();
                if (alarmService != null)
                {
                    await alarmService.RescheduleNextAlarm();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Alarm] 预约下一棒失败: {ex.Message}");
            }
        }

        // --- 修改后的各个业务方法 ---

        //保存课程（覆盖式保存，适用于同步更新）

        public async Task SaveCoursesAsync(List<Course> courses)
        {
            await Init();
            await _db.Table<Course>().Where(x => !x.IsManual).DeleteAsync();
            await _db.InsertAllAsync(courses);

            // 调用统一通知
            await NotifyWidgetAsync();
        }

        //手动添加单条课程

        public async Task SaveSingleCourseAsync(Course course)
        {
            await Init();
            await _db.InsertAsync(course);

            // 手动保存单条也通知小组件
            await NotifyWidgetAsync();
        }

        //手动删除单条课程

        public async Task DeleteCourseAsync(Course course)
        {
            await Init();
            await _db.DeleteAsync(course);

            // 删除后立刻通知小组件
            await NotifyWidgetAsync();
        }

        //一键清空所有课程（包括手动添加的课程）

        public async Task ClearAllCoursesAsync()
        {
            await Init();
            await _db.DeleteAllAsync<Course>();

            // 清空后通知（小组件会显示“暂时没课啦”）
            await NotifyWidgetAsync();
        }

        // --- 其余排序逻辑 (GetCoursesAsync, GetDayWeight, GetTimeWeight) 保持不变 ---
        public async Task<List<Course>> GetCoursesAsync()
        {
            await Init();
            var allCourses = await _db.Table<Course>().ToListAsync();
            return allCourses
                .OrderBy(c => GetDayWeight(c.DayOfWeek))
                .ThenBy(c => GetTimeWeight(c.StartTime))
                    .ToList();
        }

        private int GetDayWeight(string day) { /* ... 保持原样 ... */ return _dayOrder.IndexOf(day) == -1 ? 99 : _dayOrder.IndexOf(day); }
        private double GetTimeWeight(string timeStr)
        {
            if (string.IsNullOrWhiteSpace(timeStr)) return 0;

            try
            {
                // 1. 统一转小写并清理空格，处理像 "12.00pm" 这样的字符串
                string t = timeStr.ToLower().Trim();
                bool isPm = t.Contains("pm");
                bool isAm = t.Contains("am");

                // 2. 只保留数字和点，把 "12.00pm" 变成 "12.00"
                string digits = t.Replace("am", "").Replace("pm", "").Trim();
                var parts = digits.Split('.');

                int hour = int.Parse(parts[0]);
                int minute = parts.Length > 1 ? int.Parse(parts[1]) : 0;

                // 3. 核心修正逻辑：处理 12 小时制
                // PM 情况：除了 12 PM (中午)，其余小时数都要加 12
                if (isPm && hour != 12)
                {
                    hour += 12;
                }
                // AM 情况：12 AM (凌晨) 要变成 0 点
                else if (isAm && hour == 12)
                {
                    hour = 0;
                }

                // 4. 返回总分钟数作为权重进行排序
                return hour * 60 + minute;
            }
            catch
            {
                // 如果解析出错，返回一个极大的值排在最后，避免程序崩溃
                return 9999;
            }
        }
    }
}