using Microsoft.Maui.Controls;

namespace Schedule2._0.Services
{
    public class ThemeService
    {
        private const string ThemeSignatureKey = "BtnBgMain";

        public void ApplyTheme(int themeMode)
        {
            if (Application.Current == null) return;

            // 根据模式决定加载哪个资源字典名
            string targetKey = themeMode switch
            {
                1 => "LightTheme",
                2 => "DarkTheme",
                3 => "PinkTheme",
                4 => "FireflyTheme",
                5 => "WhiteEreCringeTheme",
                // 如果是 0 (跟随系统)，则探测系统当前真实的主题
                _ => Application.Current.RequestedTheme == AppTheme.Dark ? "DarkTheme" : "LightTheme"
            };

            var appResources = Application.Current.Resources;
            try
            {
                if (appResources.TryGetValue(targetKey, out var themeObj) && themeObj is ResourceDictionary targetTheme)
                {
                    var activeThemes = appResources.MergedDictionaries
                        .Where(d => d.ContainsKey(ThemeSignatureKey))
                        .ToList();

                    foreach (var oldTheme in activeThemes)
                    {
                        if (oldTheme == targetTheme) return; // 已经在运行则跳过
                        appResources.MergedDictionaries.Remove(oldTheme);
                    }
                    appResources.MergedDictionaries.Add(targetTheme);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Theme] 切换异常: {ex.Message}");
            }
        }
    }
}