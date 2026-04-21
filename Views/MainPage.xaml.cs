#if ANDROID
using Android.Appwidget;
using Android.Content;
using CommunityToolkit.Maui.Core;
using CommunityToolkit.Maui.Behaviors;
#endif
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui.Graphics;
using CommunityToolkit.Maui.Extensions;
using Schedule2._0.Helpers;
using Schedule2._0.Models;
using Schedule2._0.Services;

namespace Schedule2._0.Views;

public partial class MainPage : ContentPage
{
    private readonly ViewModels.MainViewModel _viewModel;
    private readonly DatabaseService _dbService;
    private readonly ConfigService _configService;

    public MainPage(DatabaseService dbService, ViewModels.MainViewModel viewModel, ConfigService configService)
    {
        InitializeComponent();

        _dbService = dbService;
        _viewModel = viewModel;
        _configService = configService;

        BindingContext = _viewModel;

        ThemeHelper.SyncStatusBar(this, _configService);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        ThemeHelper.SyncStatusBar(this, _configService);

        if (!_configService.PrivacyPolicyAccepted)
        {
            await ShowPrivacyPolicyAsync();
        }

        _viewModel.CardOpacity = _configService.CardOpacity;

        LoadBackgroundImage();

        await LoadCourses();
    }

    private void LoadBackgroundImage()
    {
        var path = _configService.BackgroundImagePath;
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            BgImage.Source = ImageSource.FromFile(path);
            BgImage.IsVisible = true;
        }
        else
        {
            BgImage.IsVisible = false;
            BgImage.Source = null;
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (HamburgerMenu.IsOpen || ActionMenu.IsOpen || CourseDetailMenu.IsOpen)
        {
            MainThread.BeginInvokeOnMainThread(async () => await CloseAllMenusAsync());
            return true;
        }

        return base.OnBackButtonPressed();
    }

    private async Task LoadCourses()
    {
        try
        {
            var sortedCourses = await _dbService.GetCoursesAsync();

            if (sortedCourses != null)
            {
                string lastDay = string.Empty;
                foreach (var course in sortedCourses)
                {
                    if (course.DayOfWeek != lastDay)
                    {
                        course.IsDayVisible = true;
                        lastDay = course.DayOfWeek;
                    }
                    else
                    {
                        course.IsDayVisible = false;
                    }
                }

                CoursesListView.ItemsSource = sortedCourses;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Error] Load courses failed: {ex.Message}");
            await DisplayAlertAsync("错误", "无法加载课表数据", "确定");
        }
    }

    private async void OnDeleteCourseClicked(object sender, EventArgs e)
    {
        var element = sender as Element;
        var course = element?.BindingContext as Course;

        if (course == null)
        {
            return;
        }

        bool confirm = await DisplayAlertAsync("确认删除", $"确定要删除 {course.Name} 吗？", "确定", "取消");
        if (!confirm)
        {
            return;
        }

        await _dbService.DeleteCourseAsync(course);
        await LoadCourses();
    }

    private async void OnCourseCardClicked(object sender, EventArgs e)
    {
        var element = sender as Element;
        var course = element?.BindingContext as Course;
        if (course == null) return;

        if (sender is VisualElement view)
        {
            await view.ScaleToAsync(0.98, 60);
            await view.ScaleToAsync(1.0, 60);
        }

        await CloseAllMenusAsync();

        var content = new Border
        {
            StrokeShape = new RoundRectangle { CornerRadius = 18 },
            BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
            Padding = 24,
            WidthRequest = 300,
            Content = new VerticalStackLayout
            {
                Spacing = 14,
                Children =
                {
                    new Label
                    {
                        Text = course.Name,
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                    },
                    CreateCourseDetailRow("\ue8b5", "时间", $"{course.StartTime} - {course.EndTime}"),
                    CreateCourseDetailRow("\ue55f", "地点", string.IsNullOrEmpty(course.Location) ? "未设置" : course.Location),
                    CreateCourseDetailRow("\ue7fd", "教师", string.IsNullOrEmpty(course.Teacher) ? "未设置" : course.Teacher),
                    CreateCourseDetailRow("\ue935", "星期", course.DayOfWeek ?? "未设置"),
                    CreateCourseDetailCloseButton()
                }
            }
        };

        await PopupWindow.ShowCustomAsync(
            host: CourseDetailMenu,
            content: content,
            animationMode: MenuAnimationMode.PopUp,
            showOverlay: true,
            horizontalAlign: LayoutOptions.Center,
            verticalAlign: LayoutOptions.Center,
            margin: new Thickness(0),
            overlayOpacity: 0.32
        );
    }

    private HorizontalStackLayout CreateCourseDetailRow(string iconGlyph, string label, string value)
    {
        return new HorizontalStackLayout
        {
            Spacing = 10,
            Children =
            {
                new Label
                {
                    Text = iconGlyph,
                    FontFamily = "MaterialSymbols",
                    FontSize = 20,
                    TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                    VerticalOptions = LayoutOptions.Center
                },
                new VerticalStackLayout
                {
                    Spacing = 2,
                    Children =
                    {
                        new Label
                        {
                            Text = label,
                            FontSize = 12,
                            TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray
                        },
                        new Label
                        {
                            Text = value,
                            FontSize = 15,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        }
                    }
                }
            }
        };
    }

    private Button CreateCourseDetailCloseButton()
    {
        var button = new Button
        {
            Text = "我知道了",
            FontSize = 15,
            FontAttributes = FontAttributes.Bold,
            BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.LightGray,
            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
            CornerRadius = 12,
            HeightRequest = 44,
            HorizontalOptions = LayoutOptions.Center,
            Shadow = null
        };

        button.Clicked += async (s, e) =>
        {
            await CourseDetailMenu.HideAsync();
        };

        return button;
    }

    private async void OnHamburgerClicked(object sender, EventArgs e)
    {
        if (ActionMenu.IsOpen)
        {
            await ActionMenu.HideAsync();
        }

        var options = new[] { "设置", "清空课表" };
        var idx = await PopupWindow.ShowAsync(
            host: HamburgerMenu,                           // MenuView 实例 - 用于承载弹窗的容器
            title: "",                                     // 弹窗标题 - 显示在顶部的文字（空字符串表示不显示标题）
            options: options,                              // 选项列表 - 弹窗内显示的按钮文本数组
            animationMode: MenuAnimationMode.SlideDown,    // 动画模式 - SlideDown(下滑), PopUp(弹出), Fade(淡入), None(无动画)
            showOverlay: true,                             // 是否显示遮罩 - true 显示半透明黑色背景
            popupWidth: 200,                              // 弹窗宽度 - null 表示自动计算，可指定具体像素值如 300
            popupHeight: null,                             // 弹窗高度 - null 表示自动计算，可指定具体像素值如 200
            horizontalAlign: LayoutOptions.End,            // 水平对齐 - Start(左), Center(中), End(右), Fill(填充)
            verticalAlign: LayoutOptions.Start,            // 垂直对齐 - Start(上), Center(中), End(下), Fill(填充)
            showSeparatorLines: true,                      // 是否显示分隔线 - true 在按钮之间显示灰色细线
            margin: new Thickness(0, 80, 18, 0),           // 弹窗边距 - (左, 上, 右, 下) 控制弹窗距离屏幕边缘的距离
            overlayOpacity: 0.2                            // 遮罩透明度 - 0.0(完全透明) 到 1.0(完全不透明)
        );

        if (idx == 0)
        {
            await HamburgerMenu.HideAsync();
            await Shell.Current.GoToAsync(nameof(SettingsPage));
        }
        else if (idx == 1)
        {
            await HamburgerMenu.HideAsync();
            bool confirm = await DisplayAlertAsync("危险操作", "确定要清空所有课程吗？此操作无法撤销。", "确定", "取消");
            if (confirm)
            {
                await _dbService.ClearAllCoursesAsync();
                await LoadCourses();
                await DisplayAlertAsync("提示", "课表已清空。", "确定");
            }
        }
    }

    private async void OnSyncClicked(object sender, EventArgs e)
    {
        if (HamburgerMenu.IsOpen)
        {
            await HamburgerMenu.HideAsync();
        }

        var options = new[] { "导入XMUM课程表", "手动导入" };
        var idx = await PopupWindow.ShowAsync(
            host: ActionMenu,                              // MenuView 实例 - 用于承载弹窗的容器
            title: "",                                     // 弹窗标题 - 显示在顶部的文字（空字符串表示不显示标题）
            options: options,                              // 选项列表 - 弹窗内显示的按钮文本数组
            animationMode: MenuAnimationMode.SlideDown,    // 动画模式 - SlideDown(下滑), PopUp(弹出), Fade(淡入), None(无动画)
            showOverlay: true,                             // 是否显示遮罩 - true 显示半透明黑色背景
            popupWidth: 300,                              // 弹窗宽度 - null 表示自动计算，可指定具体像素值如 300
            popupHeight: null,                             // 弹窗高度 - null 表示自动计算，可指定具体像素值如 200
            horizontalAlign: LayoutOptions.Center,         // 水平对齐 - Start(左), Center(中), End(右), Fill(填充)
            verticalAlign: LayoutOptions.Start,            // 垂直对齐 - Start(上), Center(中), End(下), Fill(填充)
            showSeparatorLines: true,                      // 是否显示分隔线 - true 在按钮之间显示灰色细线
            margin: new Thickness(18, 150, 18, 0),         // 弹窗边距 - (左, 上, 右, 下) 控制弹窗距离屏幕边缘的距离
            overlayOpacity: 0.2                            // 遮罩透明度 - 0.0(完全透明) 到 1.0(完全不透明)
        );

        if (idx == 0)
        {
            await ActionMenu.HideAsync();

            // 构建确认弹窗内容
            var tcs = new TaskCompletionSource<bool>();

            var messageLabel = new Label
            {
                Padding = new Thickness(20, 20, 20, 10),
                LineBreakMode = LineBreakMode.WordWrap,
                FormattedText = new FormattedString
                {
                    Spans =
                    {
                        new Span
                        {
                            Text = "即将使用app内置浏览器跳转到学校教务系统，请自行完成登录步骤。",
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            FontSize = 15
                        },
                        new Span
                        {
                            Text = "本应用不会收集任何账号信息！",
                            TextColor = Colors.Red,
                            FontSize = 15,
                            FontAttributes = FontAttributes.Bold
                        }
                    }
                }
            };

            var buttonStack = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                Padding = new Thickness(10, 0, 10, 10)
            };

            var cancelBtn = PopupWindow.CreateButton("取消", fontSize: 15);
            cancelBtn.Clicked += async (s2, e2) =>
            {
                tcs.TrySetResult(false);
                await ActionMenu.HideAsync();
            };

            var confirmBtn = PopupWindow.CreateButton("确认", fontSize: 15, fontAttributes: FontAttributes.Bold);
            confirmBtn.Clicked += async (s2, e2) =>
            {
                tcs.TrySetResult(true);
                await ActionMenu.HideAsync();
            };

            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(confirmBtn, 1);
            buttonStack.Children.Add(cancelBtn);
            buttonStack.Children.Add(confirmBtn);

            var content = new VerticalStackLayout { Spacing = 0 };
            content.Children.Add(messageLabel);
            content.Children.Add(PopupWindow.CreateSeparator());
            content.Children.Add(buttonStack);

            var card = new Border
            {
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 0,
                StrokeThickness = 0,
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
                WidthRequest = 320,
                Content = content
            };

            await PopupWindow.ShowCustomAsync(
                host: ActionMenu,
                content: card,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.3
            );

            bool confirmed = await tcs.Task;
            if (confirmed)
            {
                await Shell.Current.GoToAsync($"{nameof(LoginPage)}?school={SchoolCodes.Xmum}");
            }
        }
        else if (idx == 1)
        {
            await ActionMenu.HideAsync();
            await Shell.Current.GoToAsync(nameof(AddCoursePage));
        }
    }

    private async Task ShowPrivacyPolicyAsync()
    {
        string policyText;
        using (var stream = await FileSystem.OpenAppPackageFileAsync("PrivacyPolicy(Simplified).txt"))
        using (var reader = new StreamReader(stream))
        {
            policyText = await reader.ReadToEndAsync();
        }

        var tcs = new TaskCompletionSource<bool>();

        var scrollView = new ScrollView
        {
            MaximumHeightRequest = 420,
            Content = new Label
            {
                Text = policyText,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
            }
        };

        // 《隐私政策全文》按钮
        var fullPolicyBtn = new Button
        {
            Text = "\u300A\u9690\u79C1\u653F\u7B56\u5168\u6587\u300B",
            BackgroundColor = Colors.Transparent,
            TextColor = Colors.DodgerBlue,
            FontSize = 14,
            HorizontalOptions = LayoutOptions.Center,
            Padding = 0
        };
        fullPolicyBtn.Clicked += async (s, e) =>
        {
            await PrivacyMenu.HideAsync();
            await ShowFullPrivacyPolicyAsync();
            await ShowPrivacyPolicyAsync();
        };

        var agreeBtn = new Button
        {
            Text = "\u540C\u610F",
            BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Fill
        };
        agreeBtn.Clicked += async (s, e) =>
        {
            _configService.PrivacyPolicyAccepted = true;
            await PrivacyMenu.HideAsync();
            tcs.TrySetResult(true);
        };

        var declineBtn = new Button
        {
            Text = "\u4E0D\u540C\u610F\u5E76\u9000\u51FA",
            BackgroundColor = (Color?)Application.Current?.Resources["BtnBgSec"] ?? Colors.Gray,
            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
            HorizontalOptions = LayoutOptions.Fill
        };
        declineBtn.Clicked += (s, e) =>
        {
            Application.Current?.Quit();
        };

        var buttonGrid = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new ColumnDefinition(GridLength.Star),
                new ColumnDefinition(GridLength.Star)
            },
            ColumnSpacing = 12
        };
        Grid.SetColumn(declineBtn, 0);
        Grid.SetColumn(agreeBtn, 1);
        buttonGrid.Children.Add(declineBtn);
        buttonGrid.Children.Add(agreeBtn);

        var content = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
            Padding = 22,
            WidthRequest = 340,
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "\u9690\u79C1\u653F\u7B56\u4E0E\u670D\u52A1\u534F\u8BAE",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    scrollView,
                    PopupWindow.CreateSeparator(),
                    fullPolicyBtn,
                    buttonGrid
                }
            }
        };

        await PopupWindow.ShowCustomAsync(
            host: PrivacyMenu,
            content: content,
            animationMode: MenuAnimationMode.PopUp,
            showOverlay: true,
            horizontalAlign: LayoutOptions.Center,
            verticalAlign: LayoutOptions.Center,
            margin: new Thickness(0),
            overlayOpacity: 0.4
        );

        await tcs.Task;
    }

    private async Task ShowFullPrivacyPolicyAsync()
    {
        string fullPolicyText;
        using (var stream = await FileSystem.OpenAppPackageFileAsync("PrivacyPolicy.txt"))
        using (var reader = new StreamReader(stream))
        {
            fullPolicyText = await reader.ReadToEndAsync();
        }

        var tcs = new TaskCompletionSource<bool>();

        var scrollView = new ScrollView
        {
            MaximumHeightRequest = 500,
            Content = new Label
            {
                Text = fullPolicyText,
                FontSize = 13,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
            }
        };

        var backBtn = new Button
        {
            Text = "返回",
            WidthRequest = 120,
            BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
            TextColor = Colors.White,
            HorizontalOptions = LayoutOptions.Center
        };
        backBtn.Clicked += async (s, e) =>
        {
            await PrivacyMenu.HideAsync();
            tcs.TrySetResult(true);
        };

        var content = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
            BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
            Padding = 22,
            WidthRequest = 340,
            Content = new VerticalStackLayout
            {
                Spacing = 16,
                Children =
                {
                    new Label
                    {
                        Text = "隐私政策全文",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                        HorizontalOptions = LayoutOptions.Center
                    },
                    scrollView,
                    PopupWindow.CreateSeparator(),
                    backBtn
                }
            }
        };

        await PopupWindow.ShowCustomAsync(
            host: PrivacyMenu,
            content: content,
            animationMode: MenuAnimationMode.PopUp,
            showOverlay: true,
            horizontalAlign: LayoutOptions.Center,
            verticalAlign: LayoutOptions.Center,
            margin: new Thickness(0),
            overlayOpacity: 0.4
        );

        await tcs.Task;
    }

    private async Task CloseAllMenusAsync()
    {
        if (ActionMenu.IsOpen)
        {
            await ActionMenu.HideAsync();
        }

        if (HamburgerMenu.IsOpen)
        {
            await HamburgerMenu.HideAsync();
        }

        if (CourseDetailMenu.IsOpen)
        {
            await CourseDetailMenu.HideAsync();
        }
    }
}

