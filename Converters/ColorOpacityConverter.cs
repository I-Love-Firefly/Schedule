using System.Globalization;

namespace Schedule2._0.Converters
{
    /// <summary>
    /// 将 CardOpacity (double 0~1) 应用到 CardBg 颜色的 Alpha 通道。
    /// 用法: BackgroundColor="{Binding CardOpacity, Converter={StaticResource ColorOpacityConverter}}"
    /// ConverterParameter 为备用颜色（可选），默认从资源读取 CardBg。
    /// </summary>
    public class ColorOpacityConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            var opacity = value is double d ? (float)d : 1f;

            Color baseColor = Colors.White;
            if (Application.Current?.Resources.TryGetValue("CardBg", out var res) == true && res is Color c)
            {
                baseColor = c;
            }

            return baseColor.WithAlpha(opacity);
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
