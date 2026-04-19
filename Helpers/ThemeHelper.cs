using Microsoft.Maui.Graphics;
#if ANDROID
using Microsoft.Maui.Platform;
using Android.Views;
#endif

namespace Schedule2._0.Helpers
{
    public static class ThemeHelper
    {
        public static void SyncStatusBar(Page page, Schedule2._0.Services.ConfigService config)
        {
            // 必须在 UI 线程执行
            MainThread.BeginInvokeOnMainThread(async () =>
            {
                try
                {
                    // 稍微加长延迟，确保配置已写入磁盘且 ResourceDictionary 已生效
                    await Task.Delay(150);

                    if (Application.Current == null || config == null) return;

                    // 1. 获取最新颜色
                    Color targetColor;
                    if (Application.Current.Resources.TryGetValue("AppBg", out var colorValue) && colorValue is Color themeColor)
                    {
                        targetColor = themeColor;
                    }
                    else
                    {
                        targetColor = config.CurrentTag == "dark" ? Colors.Black : Colors.White;
                    }

                    // 2. 安卓原生暴力刷新
#if ANDROID
                    var activity = Platform.CurrentActivity;
                    var window = activity?.Window;
                    if (window != null)
                    {
                        // 清除可能干扰的标志位
                        window.ClearFlags(WindowManagerFlags.TranslucentStatus);
                        window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

                        // 强制设置颜色
                        window.SetStatusBarColor(targetColor.ToPlatform());

                        // 强制设置图标明暗 (使用最稳妥的旧版 Flags 兼容所有版本)
                        var decorView = window.DecorView;
                        int flags = (int)decorView.SystemUiVisibility;

                        bool isLightMode = config.CurrentTag != "dark";
                        if (isLightMode)
                        {
                            flags |= (int)SystemUiFlags.LightStatusBar; // 0x00000010
                        }
                        else
                        {
                            flags &= ~(int)SystemUiFlags.LightStatusBar;
                        }

                        decorView.SystemUiVisibility = (StatusBarVisibility)flags;
                    }
#endif
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[ThemeHelper Error] {ex.Message}");
                }
            });
        }
    }
}