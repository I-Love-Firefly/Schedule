using System;
using System.Globalization;
using Microsoft.Maui.Controls;
using Schedule2._0.Models;

namespace Schedule2._0.Converters
{
    public class CurrentCourseToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not Course course)
                return false;
            var now = DateTime.Now;
            var nowDay = now.DayOfWeek.ToString();
            // 兼容常见格式：Monday、周一、星期一
            bool dayMatch = string.Equals(course.DayOfWeek, nowDay, StringComparison.OrdinalIgnoreCase)
                || course.DayOfWeek.Contains(((int)now.DayOfWeek).ToString())
                || course.DayOfWeek.Contains("周" + GetChineseDay(now.DayOfWeek))
                || course.DayOfWeek.Contains("星期" + GetChineseDay(now.DayOfWeek));
            if (!dayMatch)
                return false;
            // 支持 08:00、8:00、08:00:00、8:00:00、10.00am 等格式
            if (!TryParseTime(course.StartTime, out var start) || !TryParseTime(course.EndTime, out var end))
                return false;
            var nowTime = now.TimeOfDay;
            return nowTime >= start && nowTime <= end;
        }

        private static string GetChineseDay(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "一",
                DayOfWeek.Tuesday => "二",
                DayOfWeek.Wednesday => "三",
                DayOfWeek.Thursday => "四",
                DayOfWeek.Friday => "五",
                DayOfWeek.Saturday => "六",
                DayOfWeek.Sunday => "日",
                _ => ""
            };
        }

        private static bool TryParseTime(string input, out TimeSpan time)
        {
            // 支持 08:00、8:00、08:00:00、8:00:00
            if (TimeSpan.TryParse(input, out time))
                return true;
            // 支持 10.00am/pm
            if (!string.IsNullOrEmpty(input) && (input.EndsWith("am", StringComparison.OrdinalIgnoreCase) || input.EndsWith("pm", StringComparison.OrdinalIgnoreCase)))
            {
                var clean = input.Replace("am", "", StringComparison.OrdinalIgnoreCase).Replace("pm", "", StringComparison.OrdinalIgnoreCase).Trim();
                clean = clean.Replace('.', ':');
                if (TimeSpan.TryParse(clean, out var t))
                {
                    if (input.EndsWith("pm", StringComparison.OrdinalIgnoreCase) && t.Hours < 12)
                        t = t.Add(TimeSpan.FromHours(12));
                    time = t;
                    return true;
                }
            }
            time = default;
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }
}
