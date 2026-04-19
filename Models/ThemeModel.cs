namespace Schedule2._0.Models
{
    // 定义严格的标签枚举
    public enum ThemeTag { Light, Dark }

    public class ThemeConfig
    {       
        public string ResourceKey { get; set; } // 对应 XAML 中的 x:Key (如 "PinkTheme")

        public string DisplayName { get; set; } // 显示名称 (如 "猛男粉")

        public ThemeTag Tag { get; set; }       // 主题所属标签
   
        public string TagString => Tag.ToString().ToLower();    // 辅助属性：返回小写字符串 "light" 或 "dark" 方便前端判定

        public bool NeedsConfirmation { get; set; }

        public int Mode { get; set; }
    }
}