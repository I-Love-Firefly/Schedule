using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views; // 必须引用这个命名空间

namespace Schedule2._0
{
    [Activity(Label = "课表助手", Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // --- 状态栏优化逻辑开始 ---

            // 1. 设置状态栏透明（让内容可以向上延伸到状态栏区域）
            Window.SetFlags(WindowManagerFlags.LayoutNoLimits, WindowManagerFlags.LayoutNoLimits);

            // 2. 清除透明状态栏的标志（如果需要完全自定义颜色，有时需要先清除这个）
            Window.ClearFlags(WindowManagerFlags.TranslucentStatus);

            // 3. 允许绘制系统栏背景
            Window.AddFlags(WindowManagerFlags.DrawsSystemBarBackgrounds);

            // 4. (可选) 如果你希望启动时状态栏文字是深色的（针对浅色背景）
            // 注意：这部分通常建议配合之前提到的 StatusBarBehavior 来动态控制
            // if (Build.VERSION.SdkInt >= BuildVersionCodes.M) 
            // {
            //    Window.DecorView.SystemUiVisibility = (StatusBarVisibility)SystemUiFlags.LightStatusBar;
            // }

            // --- 状态栏优化逻辑结束 ---
        }
    }
}