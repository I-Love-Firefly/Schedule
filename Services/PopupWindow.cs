using System.Threading.Tasks;
using Microsoft.Maui;
using Microsoft.Maui.Controls;
using Schedule2._0.Views;
using Schedule2._0.Models;

namespace Schedule2._0.Services
{
    /// <summary>
    /// 弹窗配置类，封装所有弹窗参数
    /// </summary>
    public class PopupConfig
    {
        public MenuAnimationMode AnimationMode { get; set; } = MenuAnimationMode.SlideDown;
        public bool ShowOverlay { get; set; } = true;
        public double OverlayOpacity { get; set; } = 0.42;
        public double? PopupWidth { get; set; } = null;
        public double? PopupHeight { get; set; } = null;
        public LayoutOptions HorizontalAlign { get; set; } = LayoutOptions.Center;
        public LayoutOptions VerticalAlign { get; set; } = LayoutOptions.Start;
        public Thickness Margin { get; set; } = new Thickness(18, 150, 18, 0);
        public bool ShowSeparatorLines { get; set; } = true;
    }

    /// <summary>
    /// 弹窗按钮配置类
    /// </summary>
    public class PopupButtonConfig
    {
        public string Text { get; set; } = "";
        public Color BackgroundColor { get; set; } = Colors.Transparent;
        public Color? TextColor { get; set; } = null;
        public double HeightRequest { get; set; } = 52;
        public FontAttributes FontAttributes { get; set; } = FontAttributes.None;
        public double FontSize { get; set; } = 14;
    }

    /// <summary>
    /// 弹窗工具类 - 统一管理所有弹窗显示逻辑
    /// </summary>
    public static class PopupWindow
    {
        /// <summary>
        /// 生成弹窗内的按钮
        /// </summary>
        /// <param name="text">按钮文本</param>
        /// <param name="backgroundColor">背景色（默认透明）</param>
        /// <param name="textColor">文字颜色（默认使用主题色）</param>
        /// <param name="heightRequest">按钮高度</param>
        /// <param name="fontAttributes">字体样式</param>
        /// <param name="fontSize">字体大小</param>
        /// <returns>配置好的按钮</returns>
        public static Button CreateButton(
            string text,
            Color? backgroundColor = null,
            Color? textColor = null,
            double heightRequest = 52,
            FontAttributes fontAttributes = FontAttributes.None,
            double fontSize = 14)
        {
            return new Button
            {
                Text = text,
                BackgroundColor = backgroundColor ?? Colors.Transparent,
                TextColor = textColor ?? (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HeightRequest = heightRequest,
                HorizontalOptions = LayoutOptions.Fill,
                FontAttributes = fontAttributes,
                FontSize = fontSize
            };
        }

        /// <summary>
        /// 通过配置对象生成按钮
        /// </summary>
        public static Button CreateButton(PopupButtonConfig config)
        {
            if (config == null) throw new System.ArgumentNullException(nameof(config));

            return new Button
            {
                Text = config.Text,
                BackgroundColor = config.BackgroundColor,
                TextColor = config.TextColor ?? (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HeightRequest = config.HeightRequest,
                HorizontalOptions = LayoutOptions.Fill,
                FontAttributes = config.FontAttributes,
                FontSize = config.FontSize
            };
        }

        /// <summary>
        /// 生成分隔线
        /// </summary>
        /// <param name="height">分隔线高度(粗细)</param>
        /// <param name="color">分隔线颜色</param>
        /// <param name="opacity">分隔线透明度</param>
        /// <param name="horizontalMargin">分隔线左右边距,控制线条长度不触碰边缘</param>
        public static BoxView CreateSeparator(
            double height = 1,
            Color? color = null,
            double opacity = 0.9,
            double horizontalMargin = 20)
        {
            return new BoxView
            {
                HeightRequest = height,
                Color = color ?? Colors.LightGray,
                Opacity = opacity,
                Margin = new Thickness(horizontalMargin, 0)
            };
        }

        /// <summary>
        /// 统一的弹窗显示方法 - 通过单独参数传递（公式化调用）
        /// </summary>
        /// <param name="host">MenuView 实例</param>
        /// <param name="title">弹窗标题</param>
        /// <param name="options">选项列表</param>
        /// <param name="animationMode">动画模式</param>
        /// <param name="showOverlay">是否显示遮罩</param>
        /// <param name="popupWidth">弹窗宽度（null = 自动）</param>
        /// <param name="popupHeight">弹窗高度（null = 自动）</param>
        /// <param name="horizontalAlign">水平对齐方式</param>
        /// <param name="verticalAlign">垂直对齐方式</param>
        /// <param name="showSeparatorLines">是否显示分隔线</param>
        /// <param name="margin">弹窗边距</param>
        /// <param name="overlayOpacity">遮罩透明度</param>
        /// <returns>用户选择的选项索引</returns>
        public static Task<int> ShowAsync(
            MenuView host,
            string title,
            System.Collections.Generic.IEnumerable<string> options,
            MenuAnimationMode animationMode = MenuAnimationMode.SlideDown,
            bool showOverlay = true,
            double? popupWidth = null,
            double? popupHeight = null,
            LayoutOptions horizontalAlign = default,
            LayoutOptions verticalAlign = default,
            bool showSeparatorLines = true,
            Thickness? margin = null,
            double overlayOpacity = 0.42)
        {
            var config = new PopupConfig
            {
                AnimationMode = animationMode,
                ShowOverlay = showOverlay,
                OverlayOpacity = overlayOpacity,
                PopupWidth = popupWidth,
                PopupHeight = popupHeight,
                HorizontalAlign = horizontalAlign == default ? LayoutOptions.Center : horizontalAlign,
                VerticalAlign = verticalAlign == default ? LayoutOptions.Start : verticalAlign,
                Margin = margin ?? new Thickness(18, 150, 18, 0),
                ShowSeparatorLines = showSeparatorLines
            };

            return ShowAsync(host, title, options, config);
        }

        /// <summary>
        /// 统一的弹窗显示方法 - 通过配置对象传递所有参数
        /// </summary>
        /// <param name="host">MenuView 实例</param>
        /// <param name="title">弹窗标题（可为空）</param>
        /// <param name="options">选项列表</param>
        /// <param name="config">弹窗配置对象</param>
        /// <returns>用户选择的选项索引</returns>
        public static Task<int> ShowAsync(MenuView host, string title, System.Collections.Generic.IEnumerable<string> options, PopupConfig config)
        {
            if (host == null) throw new System.ArgumentNullException(nameof(host));
            if (config == null) throw new System.ArgumentNullException(nameof(config));

            // 应用配置到 MenuView
            host.HorizontalAlign = config.HorizontalAlign;
            host.VerticalAlign = config.VerticalAlign;
            host.ContentMargin = config.Margin;
            host.ShowOverlay = config.ShowOverlay;

            // 构建内容
            var vstack = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(0) };

            // 添加标题（如果有）
            if (!string.IsNullOrWhiteSpace(title))
            {
                var titleLabel = new Label
                {
                    Text = title,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    Margin = new Thickness(0, 6, 0, 8)
                };
                vstack.Children.Add(titleLabel);
            }

            // 添加选项按钮
            var list = options.ToList();
            var tcs = new TaskCompletionSource<int>();

            for (int i = 0; i < list.Count; i++)
            {
                var text = list[i];
                var btn = CreateButton(text);

                int captured = i;
                btn.Clicked += async (s, e) =>
                {
                    tcs.TrySetResult(captured);
                    await host.HideAsync();
                };

                vstack.Children.Add(btn);

                // 添加分隔线（如果启用且不是最后一个按钮）
                if (config.ShowSeparatorLines && i < list.Count - 1)
                {
                    vstack.Children.Add(CreateSeparator());
                }
            }

            // 创建弹窗容器
            var card = new Border
            {
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 0,
                StrokeThickness = 0,
                VerticalOptions = LayoutOptions.Start,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 12 }
            };

            // 应用宽度和高度（如果指定）
            if (config.PopupWidth.HasValue)
                card.WidthRequest = config.PopupWidth.Value;
            if (config.PopupHeight.HasValue)
                card.HeightRequest = config.PopupHeight.Value;

            card.Content = vstack;

            // 显示弹窗
            _ = host.ShowAsync(card, config.Margin, config.AnimationMode, config.ShowOverlay, config.OverlayOpacity);

            return tcs.Task;
        }

        /// <summary>
        /// 显示自定义内容的弹窗
        /// </summary>
        /// <param name="host">MenuView 实例</param>
        /// <param name="content">自定义内容视图</param>
        /// <param name="animationMode">动画模式</param>
        /// <param name="showOverlay">是否显示遮罩</param>
        /// <param name="horizontalAlign">水平对齐</param>
        /// <param name="verticalAlign">垂直对齐</param>
        /// <param name="margin">边距</param>
        /// <param name="overlayOpacity">遮罩透明度</param>
        public static Task ShowCustomAsync(
            MenuView host,
            View content,
            MenuAnimationMode animationMode = MenuAnimationMode.SlideDown,
            bool showOverlay = true,
            LayoutOptions horizontalAlign = default,
            LayoutOptions verticalAlign = default,
            Thickness? margin = null,
            double overlayOpacity = 0.42)
        {
            if (host == null) throw new System.ArgumentNullException(nameof(host));
            if (content == null) throw new System.ArgumentNullException(nameof(content));

            host.HorizontalAlign = horizontalAlign == default ? LayoutOptions.Center : horizontalAlign;
            host.VerticalAlign = verticalAlign == default ? LayoutOptions.Start : verticalAlign;
            host.ContentMargin = margin ?? new Thickness(18, 150, 18, 0);

            return host.ShowAsync(content, host.ContentMargin, animationMode, showOverlay, overlayOpacity);
        }
    }
}
