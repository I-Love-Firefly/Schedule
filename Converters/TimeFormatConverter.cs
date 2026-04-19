using System.Globalization;

namespace Schedule2._0.Converters
{
    /// <summary>
    /// 时间格式转换器: 将 "10.00am" 转换为 "10:00 A.M."
    /// </summary>
    public class TimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not string timeStr || string.IsNullOrWhiteSpace(timeStr))
                return value;

            try
            {
                // 移除所有空格并转换为小写
                timeStr = timeStr.Trim().ToLower();

                // 提取时间部分和AM/PM部分
                string timePart = "";
                string periodPart = "";

                if (timeStr.EndsWith("am"))
                {
                    periodPart = "A.M.";
                    timePart = timeStr.Substring(0, timeStr.Length - 2);
                }
                else if (timeStr.EndsWith("pm"))
                {
                    periodPart = "P.M.";
                    timePart = timeStr.Substring(0, timeStr.Length - 2);
                }
                else
                {
                    return value; // 无法识别的格式,返回原值
                }

                // 将点号替换为冒号
                timePart = timePart.Replace('.', ':');

                // 确保分钟是两位数
                var parts = timePart.Split(':');
                if (parts.Length == 2)
                {
                    string hour = parts[0];
                    string minute = parts[1].PadLeft(2, '0');
                    return $"{hour}:{minute} {periodPart}";
                }

                return value;
            }
            catch
            {
                return value; // 出错时返回原值
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
