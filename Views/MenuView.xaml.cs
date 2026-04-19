using Schedule2._0.Models;
using CommunityToolkit.Maui.Alerts;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;

namespace Schedule2._0.Views;

public partial class MenuView : ContentView
{
    public static readonly BindableProperty HorizontalAlignProperty =
        BindableProperty.Create(nameof(HorizontalAlign), typeof(LayoutOptions), typeof(MenuView), LayoutOptions.Center);

    public static readonly BindableProperty VerticalAlignProperty =
        BindableProperty.Create(nameof(VerticalAlign), typeof(LayoutOptions), typeof(MenuView), LayoutOptions.Center);

    public static readonly BindableProperty CloseOnBackgroundTapProperty =
        BindableProperty.Create(nameof(CloseOnBackgroundTap), typeof(bool), typeof(MenuView), true);

    public LayoutOptions HorizontalAlign
    {
        get => (LayoutOptions)GetValue(HorizontalAlignProperty);
        set => SetValue(HorizontalAlignProperty, value);
    }

    public LayoutOptions VerticalAlign
    {
        get => (LayoutOptions)GetValue(VerticalAlignProperty);
        set => SetValue(VerticalAlignProperty, value);
    }

    public bool CloseOnBackgroundTap
    {
        get => (bool)GetValue(CloseOnBackgroundTapProperty);
        set => SetValue(CloseOnBackgroundTapProperty, value);
    }

    public bool IsOpen { get; private set; }
    private bool _isAnimating;

    public static readonly BindableProperty ContentMarginProperty =
        BindableProperty.Create(nameof(ContentMargin), typeof(Thickness), typeof(MenuView), new Thickness(18, 120, 18, 0));

    public Thickness ContentMargin
    {
        get => (Thickness)GetValue(ContentMarginProperty);
        set => SetValue(ContentMarginProperty, value);
    }

    // 控制是否允许显示遮罩（可由外部设置）
    public static readonly BindableProperty ShowOverlayProperty =
        BindableProperty.Create(nameof(ShowOverlay), typeof(bool), typeof(MenuView), true);

    public bool ShowOverlay
    {
        get => (bool)GetValue(ShowOverlayProperty);
        set => SetValue(ShowOverlayProperty, value);
    }

    // 记录上一次显示的配置，用于 Toggle
    private MenuAnimationMode _lastMode = MenuAnimationMode.PopUp;
    private bool _lastShowOverlay = true;
    private double _lastOpacity = 0.42;

    public MenuView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 显示自定义内容的弹窗。传入任意 View（可以包含文本、图片、按钮等）。
    /// 可选传入 margin 用于定位（距离屏幕左，上，右，下）。
    /// </summary>
    public async Task ShowAsync(View content, Thickness? margin = null, MenuAnimationMode mode = MenuAnimationMode.SlideDown, bool showOverlay = true, double opacity = 0.42)
    {
        // set content into host
        ContentHost.Content = content;
        if (margin.HasValue) ContentMargin = margin.Value;
        await ShowAsync(mode, showOverlay, opacity);
    }

    private async void OnBackgroundTapped(object? sender, EventArgs e)
    {
        if (!CloseOnBackgroundTap || _isAnimating || !IsOpen) return;
        await HideAsync();
    }

    public async Task ShowAsync(MenuAnimationMode mode = MenuAnimationMode.PopUp, bool showOverlay = true, double opacity = 0.42)
    {
        if (_isAnimating || IsOpen) return;
        _isAnimating = true;

        // 如果外部通过 ShowOverlay 属性关闭遮罩，则强制不显示遮罩
        var finalShowOverlay = showOverlay && ShowOverlay;
        // 保存最后一次配置
        _lastMode = mode;
        _lastShowOverlay = finalShowOverlay;
        _lastOpacity = opacity;


        // prepare overlay and content initial state
        Overlay.Opacity = 0;
        ContentHost.Opacity = 0;
        this.Opacity = 0.99;

        // ensure menu view is visible before measuring/animating
        IsVisible = true;
        InputTransparent = false;

        // measure content to compute a proper slide-in offset (so it comes from above the card)
        try
        {
            var measure = ContentHost.Measure(double.PositiveInfinity, double.PositiveInfinity, MeasureFlags.None);
            var measuredHeight = measure.Request.Height;

            switch (mode)
            {
                case MenuAnimationMode.SlideDown:
                    // start slightly above final position by the content height + small gap
                    ContentHost.TranslationY = -measuredHeight - 8;
                    ContentHost.Scale = 1;
                    break;
                case MenuAnimationMode.PopUp:
                    ContentHost.Scale = 0.6;
                    ContentHost.TranslationY = 0;
                    break;
                default:
                    ContentHost.TranslationY = 0;
                    ContentHost.Scale = 1;
                    break;
            }
        }
        catch
        {
            // fallback offsets
            if (mode == MenuAnimationMode.SlideDown)
            {
                ContentHost.TranslationY = -150;
            }
            else if (mode == MenuAnimationMode.PopUp)
            {
                ContentHost.Scale = 0.6;
                ContentHost.TranslationY = 0;
            }
        }

        //await App.Current.MainPage.DisplayAlert("调试数值", $"遮罩透明度: {Overlay.Opacity}\n容器透明度: {ContentHost.Opacity}", "收到");

        await Dispatcher.DispatchAsync(async () =>
        {
            if (mode == MenuAnimationMode.SlideDown) ContentHost.TranslationY = -200;
            else ContentHost.Scale = 0.5;

            ContentHost.InvalidateMeasure();


            var animations = new List<Task>();
            // 遮罩动画
            if (finalShowOverlay)
            {
                Overlay.IsVisible = true;
                animations.Add(Overlay.FadeTo(opacity, 250));
            }
            //animations.Add(this.FadeTo(1, 300));

            // 根据模式准备进入动画
            switch (mode)
            {
                case MenuAnimationMode.SlideDown:
                    animations.Add(ContentHost.TranslateTo(0, 0, 400, Easing.CubicOut));
                    animations.Add(ContentHost.FadeTo(1, 300));
                    //await App.Current.MainPage.DisplayAlert("调试", "进入下拉菜单动画方法", "收到");
                    break;
                case MenuAnimationMode.PopUp:
                    animations.Add(ContentHost.ScaleTo(1, 250, Easing.SpringOut));
                    animations.Add(ContentHost.FadeTo(1, 100));
                    break;
                case MenuAnimationMode.Fade:
                    animations.Add(ContentHost.FadeTo(1, 250));
                    break;
            }

            await Task.WhenAll(animations);

            IsOpen = true;
            _isAnimating = false;
        });
    }

    public async Task HideAsync()
    {
        if (_isAnimating || !IsOpen) return;
        _isAnimating = true;

        // 1. 执行退出动画
        var animations = new List<Task>
        {
            ContentHost.FadeTo(0, 180, Easing.CubicIn),
            Overlay.FadeTo(0, 180)
        };

        await Task.WhenAll(animations);

        // 2. 【核心修复】重置所有几何属性
        // 确保下一次打开时，它是从“干净”的状态开始计算
        ContentHost.TranslationY = 0;
        ContentHost.Scale = 1;
        ContentHost.Opacity = 0;
        Overlay.Opacity = 0;

        // 3. 彻底释放状态
        this.IsVisible = false;
        this.InputTransparent = true;

        // 清理内容，防止下次打开时残留
        try { ContentHost.Content = null; } catch { }

        IsOpen = false;
        _isAnimating = false;
    }

    /// <summary>
    /// 显示一个仅包含按钮的圆角弹窗。传入若干按钮文本，方法返回被点击按钮的索引（从0开始）。
    /// 点击遮罩不会关闭对话框，遮罩用于阻止点击穿透。
    /// </summary>
    public async Task<int> ShowActionDialogAsync(string title, IEnumerable<string> options, double overlayOpacity = 0.42)
    {
        var tcs = new TaskCompletionSource<int>();

        // Title label
        var titleLabel = new Label
        {
            Text = title,
            HorizontalOptions = LayoutOptions.Center,
            FontAttributes = FontAttributes.Bold,
            Margin = new Thickness(0, 6, 0, 8)
        };

        var vstack = new VerticalStackLayout { Spacing = 0, Padding = new Thickness(0) };
        vstack.Children.Add(titleLabel);

        int idx = 0;
        var list = options.ToList();
        for (int i = 0; i < list.Count; i++)
        {
            var text = list[i];
            var btn = new Button
            {
                Text = text,
                BackgroundColor = Colors.Transparent,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HeightRequest = 52,
                HorizontalOptions = LayoutOptions.Fill
            };

            int captured = i;
            btn.Clicked += async (s, e) =>
            {
                tcs.TrySetResult(captured);
                await HideAsync();
            };

            vstack.Children.Add(btn);

            if (i < list.Count - 1)
            {
                vstack.Children.Add(new BoxView { HeightRequest = 1, Color = Colors.LightGray, Opacity = 0.9 });
            }
        }

        var card = new Border
        {
            BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
            Padding = 0,
            StrokeThickness = 0,
            VerticalOptions = LayoutOptions.Start,
            StrokeShape = new RoundRectangle { CornerRadius = 12 }
        };

        card.Content = vstack;

        ContentHost.Content = card;

        await ShowAsync(MenuAnimationMode.SlideDown, true, overlayOpacity);

        var result = await tcs.Task;

        try { ContentHost.Content = null; } catch { }

        return result;
    }

    public Task<int> ShowActionDialogAsync(string title, params string[] options)
    {
        return ShowActionDialogAsync(title, (IEnumerable<string>)options, 0.42);
    }

    public async Task ToggleAsync(MenuAnimationMode? mode = null, bool? showOverlay = null, double? opacity = null)
    {
        if (IsOpen)
            await HideAsync();
        else
            await ShowAsync(mode ?? _lastMode, showOverlay ?? _lastShowOverlay, opacity ?? _lastOpacity);
    }
}