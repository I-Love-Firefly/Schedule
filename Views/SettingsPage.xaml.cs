using Schedule2._0.Helpers;
using Schedule2._0.Models;
using Schedule2._0.Services;
using Schedule2._0.ViewModels;
using Microsoft.Maui.Controls.Shapes;

namespace Schedule2._0.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly ConfigService _configService;
        private string? _pendingThemeMode;
        private TaskCompletionSource<bool>? _isolationTcs;
        private TaskCompletionSource<bool>? _eyeProtectionTcs;
        private string _bgImageStatusText = "选择背景图";

        private IDispatcherTimer? _luxTimer;

        public SettingsPage(SettingsViewModel viewModel, ConfigService configService)
        {
            InitializeComponent();

            _configService = configService;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            ThemeHelper.SyncStatusBar(this, _configService);
            LightSensorService.Instance.Start();

            _luxTimer = Dispatcher.CreateTimer();
            _luxTimer.Interval = TimeSpan.FromMilliseconds(500);
            _luxTimer.Tick += (s, e) =>
            {
                LuxLabel.Text = $"当前Lux值: {LightSensorService.Instance.GetCurrentLux():F0}";
            };
            _luxTimer.Start();

            // Card opacity is now in the OtherSettings popup

            RefreshBgImageStatusText();

            var otherSettingsSection = this.FindByName<Layout>("OtherSettingsSection");
            if (otherSettingsSection != null)
            {
                otherSettingsSection.IsVisible = _configService.IsVIP();
            }

            await Task.Yield();
            await Task.Delay(50);
        }

        private void RefreshBgImageStatusText()
        {
            _bgImageStatusText = !string.IsNullOrEmpty(_configService.BackgroundImagePath) && File.Exists(_configService.BackgroundImagePath)
                ? "已设置背景图（点击更换）"
                : "选择背景图";
        }

        private async void OnOtherSettingsClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();
            RefreshBgImageStatusText();

            var bgStatusLabel = new Label
            {
                Text = _bgImageStatusText,
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
            };

            var bgItem = new Border
            {
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 14,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 14 },
                Content = new HorizontalStackLayout
                {
                    Spacing = 10,
                    Children =
                    {
                        new Label { Text = "🖼️", FontSize = 18, VerticalOptions = LayoutOptions.Center },
                        bgStatusLabel
                    }
                }
            };

            bgItem.GestureRecognizers.Add(new TapGestureRecognizer
            {
                Command = new Command(async () =>
                {
                    await OtherSettingsMenu.HideAsync();
                    OnBackgroundImageClicked(bgItem, EventArgs.Empty);
                })
            });

            // 课程卡片不透明度
            var opacityLabel = new Label
            {
                Text = $"{(int)(_configService.CardOpacity * 100)}%",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                VerticalOptions = LayoutOptions.Center,
                WidthRequest = 45,
                HorizontalTextAlignment = TextAlignment.End
            };

            var opacitySlider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = _configService.CardOpacity,
                MinimumTrackColor = (Color?)Application.Current?.Resources["TextAccent"] ?? Colors.Blue,
                MaximumTrackColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                ThumbColor = (Color?)Application.Current?.Resources["TextAccent"] ?? Colors.Blue,
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.Fill
            };
            opacitySlider.ValueChanged += (s, args) =>
            {
                var value = Math.Round(args.NewValue, 2);
                _configService.CardOpacity = value;
                opacityLabel.Text = $"{(int)(value * 100)}%";
            };

            var opacityItem = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "卡片不透明度",
                        FontSize = 14,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                        VerticalOptions = LayoutOptions.Center
                    }
                }
            };
            Grid.SetColumn(opacitySlider, 1);
            Grid.SetColumn(opacityLabel, 2);
            opacityItem.Children.Add(opacitySlider);
            opacityItem.Children.Add(opacityLabel);

            var closeBtn = new Button
            {
                Text = "关闭",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.LightGray,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                CornerRadius = 12,
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill,
                Shadow = null
            };
            closeBtn.Clicked += async (s, args) => await OtherSettingsMenu.HideAsync();

            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 22,
                WidthRequest = 320,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    Children =
                    {
                        new Label
                        {
                            Text = "其他设置",
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        },
                        bgItem,
                        opacityItem,
                        closeBtn
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: OtherSettingsMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.36
            );
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        private void OnCardOpacityChanged(object sender, ValueChangedEventArgs e)
        {
            var value = Math.Round(e.NewValue, 2);
            _configService.CardOpacity = value;
        }

        private async void OnBackgroundImageClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            bool hasImage = !string.IsNullOrEmpty(_configService.BackgroundImagePath)
                            && File.Exists(_configService.BackgroundImagePath);

            if (hasImage)
            {
                var tcs = new TaskCompletionSource<int>();
                var vstack = new VerticalStackLayout { Spacing = 14 };

                var titleLabel = new Label
                {
                    Text = "背景图",
                    FontSize = 20,
                    HorizontalOptions = LayoutOptions.Center,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                };
                vstack.Children.Add(titleLabel);

                var options = new[] { "更换背景图", "裁剪", "清除背景图" };
                for (int i = 0; i < options.Length; i++)
                {
                    var btn = PopupWindow.CreateButton(options[i]);
                    btn.CornerRadius = 12;
                    btn.BackgroundColor = (Color?)Application.Current?.Resources["BtnBgSec"] ?? Colors.LightGray;
                    int captured = i;
                    btn.Clicked += async (s, args) =>
                    {
                        tcs.TrySetResult(captured);
                        await BgActionMenu.HideAsync();
                    };
                    vstack.Children.Add(btn);
                }

                var cancelBtn = PopupWindow.CreateButton("取消");
                cancelBtn.BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.LightGray;
                cancelBtn.FontAttributes = FontAttributes.Bold;
                cancelBtn.CornerRadius = 12;
                cancelBtn.Clicked += async (s, args) =>
                {
                    tcs.TrySetResult(-1);
                    await BgActionMenu.HideAsync();
                };
                vstack.Children.Add(cancelBtn);

                var card = new Border
                {
                    BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                    Padding = 22,
                    StrokeThickness = 0,
                    WidthRequest = 320,
                    VerticalOptions = LayoutOptions.Start,
                    StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                    Content = vstack
                };

                await PopupWindow.ShowCustomAsync(
                    BgActionMenu,
                    card,
                    animationMode: MenuAnimationMode.PopUp,
                    verticalAlign: LayoutOptions.Center,
                    margin: new Thickness(0),
                    overlayOpacity: 0.36);

                var selected = await tcs.Task;

                if (selected == 2) // 清除背景图
                {
                    try { File.Delete(_configService.BackgroundImagePath); } catch { }
                    _configService.BackgroundImagePath = string.Empty;
                    _configService.BackgroundImageScale = 1.0;
                    _configService.BackgroundImageOffsetX = 0.0;
                    _configService.BackgroundImageOffsetY = 0.0;
                    RefreshBgImageStatusText();
                    return;
                }
                if (selected == 1) // 裁剪
                {
                    await ShowBgCropPopupAsync();
                    return;
                }
                if (selected != 0) // 不是"更换背景图"（包括"取消"和点击遮罩）
                    return;
            }

            var customFileType = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                { DevicePlatform.Android, new[] { "image/jpeg", "image/png", "image/gif" } },
                { DevicePlatform.iOS, new[] { "public.jpeg", "public.png", "com.compuserve.gif" } },
                { DevicePlatform.WinUI, new[] { ".jpg", ".jpeg", ".png", ".gif" } }
            });

            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "选择背景图片",
                FileTypes = customFileType
            });

            if (result == null)
                return;

            var destDir = System.IO.Path.Combine(FileSystem.AppDataDirectory, "backgrounds");
            Directory.CreateDirectory(destDir);

            var destPath = System.IO.Path.Combine(destDir, "bg_image" + System.IO.Path.GetExtension(result.FileName));

            using (var sourceStream = await result.OpenReadAsync())
            using (var destStream = File.Create(destPath))
            {
                await sourceStream.CopyToAsync(destStream);
            }

            _configService.BackgroundImagePath = destPath;
            _configService.BackgroundImageScale = 1.0;
            _configService.BackgroundImageOffsetX = 0.0;
            _configService.BackgroundImageOffsetY = 0.0;
            RefreshBgImageStatusText();

            await ShowBgCropPopupAsync();
        }

        protected override bool OnBackButtonPressed()
        {
            if (AboutMenu.IsOpen || isolation.IsOpen || EyeProtectionMenu.IsOpen || FeedbackMenu.IsOpen || PrivacyPolicyMenu.IsOpen || LuxInfoMenu.IsOpen || BgCropMenu.IsOpen || OtherSettingsMenu.IsOpen)
            {
                MainThread.BeginInvokeOnMainThread(async () => await CloseDialogsAsync());
                return true;
            }

            return base.OnBackButtonPressed();
        }

        private void OnSystemThemeClicked(object sender, EventArgs e)
        {
            ExecuteSwitch("System");
        }

        private async void OnThemeButtonClicked(object sender, EventArgs e)
        {
            var themeKey = (sender as Button)?.CommandParameter?.ToString();
            if (string.IsNullOrWhiteSpace(themeKey))
            {
                return;
            }

            _pendingThemeMode = themeKey;

            if (BindingContext is not SettingsViewModel vm)
            {
                return;
            }

            var targetTheme = vm.GetThemeByKey(themeKey);
            if (targetTheme == null)
            {
                return;
            }

            if (targetTheme.NeedsConfirmation && _configService.AppTheme != 5)
            {
                await CloseDialogsAsync();
                await ShowIsolationDialogAsync();
                return;
            }

            if (vm.CurrentThemeTag == "dark" && targetTheme.TagString == "light" && LightSensorService.Instance.IsDim())
            {
                await CloseDialogsAsync();
                await ShowEyeProtectionDialogAsync();
                return;
            }

            ExecuteSwitch(themeKey);
        }

        private async Task ShowIsolationDialogAsync()
        {
            _isolationTcs = new TaskCompletionSource<bool>();

            // 构建"爱上雷神！"确认对话框内容
            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 22,
                WidthRequest = 320,
                Content = new VerticalStackLayout
                {
                    Spacing = 18,
                    Children =
                    {
                        new Label
                        {
                            Text = "爱上雷神！",
                            FontSize = 22,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        },
                        new Image
                        {
                            Source = "bushi.png",
                            WidthRequest = 180,
                            HeightRequest = 180,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "真的要切换到这个雷霆主题吗？",
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitionCollection
                            {
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                            },
                            ColumnSpacing = 14,
                            Children =
                            {
                                CreateDialogButton("那算了", false, 0),
                                CreateDialogButton("那我高低看两眼", true, 1)
                            }
                        }
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: isolation,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.36
            );

            var result = await _isolationTcs.Task;
            if (result)
            {
                ExecuteSwitch(_pendingThemeMode);
            }
            else
            {
                _pendingThemeMode = null;
            }
        }

        private async Task ShowEyeProtectionDialogAsync()
        {
            _eyeProtectionTcs = new TaskCompletionSource<bool>();

            // 构建"保护眼睛"提示对话框内容
            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 22,
                WidthRequest = 320,
                Content = new VerticalStackLayout
                {
                    Spacing = 18,
                    Children =
                    {
                        new Label
                        {
                            Text = "当心眼睛！",
                            FontSize = 22,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "当前环境偏暗，仍要切换到亮色主题吗？",
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitionCollection
                            {
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                                new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                            },
                            ColumnSpacing = 14,
                            Children =
                            {
                                CreateEyeProtectionButton("还是算了吧", false, 0),
                                CreateEyeProtectionButton("就要换", true, 1)
                            }
                        }
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: EyeProtectionMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.36
            );

            var result = await _eyeProtectionTcs.Task;
            if (result)
            {
                ExecuteSwitch(_pendingThemeMode);
            }
            else
            {
                _pendingThemeMode = null;
            }
        }

        private Button CreateDialogButton(string text, bool isConfirm, int column)
        {
            var button = new Button
            {
                Text = text,
                BackgroundColor = isConfirm
                    ? (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue
                    : (Color?)Application.Current?.Resources["BtnBgSec"] ?? Colors.Gray,
                TextColor = isConfirm ? Colors.White : (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
            };

            Grid.SetColumn(button, column);

            button.Clicked += async (s, e) =>
            {
                await isolation.HideAsync();
                _isolationTcs?.TrySetResult(isConfirm);
            };

            return button;
        }

        private Button CreateEyeProtectionButton(string text, bool isConfirm, int column)
        {
            var button = new Button
            {
                Text = text,
                BackgroundColor = isConfirm
                    ? (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue
                    : (Color?)Application.Current?.Resources["BtnBgSec"] ?? Colors.Gray,
                TextColor = isConfirm ? Colors.White : (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
            };

            Grid.SetColumn(button, column);

            button.Clicked += async (s, e) =>
            {
                await EyeProtectionMenu.HideAsync();
                _eyeProtectionTcs?.TrySetResult(isConfirm);
            };

            return button;
        }

        private void ExecuteSwitch(string? mode)
        {
            if (BindingContext is SettingsViewModel vm && !string.IsNullOrWhiteSpace(mode))
            {
                vm.SwitchThemeCommand.Execute(mode);
                ThemeHelper.SyncStatusBar(this, _configService);
            }
        }

        private async void OnAboutButtonClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();

            // 读取 AboutSchedule.txt 内容
            string aboutText;
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("AboutSchedule.txt");
                using var reader = new StreamReader(stream);
                aboutText = await reader.ReadToEndAsync();
            }
            catch
            {
                aboutText = "无法加载关于信息。";
            }

            // 构建"关于"信息对话框内容
            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 24,
                WidthRequest = 300,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new ScrollView
                        {
                            MaximumHeightRequest = 400,
                            Content = new Label
                            {
                                Text = aboutText,
                                FontSize = 14,
                                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                                LineBreakMode = LineBreakMode.WordWrap
                            }
                        },
                        CreateAboutButton()
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: AboutMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.32
            );
        }

        private Button CreateAboutButton()
        {
            var button = new Button
            {
                Text = "确定",
                WidthRequest = 120,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };

            button.Clicked += async (s, e) =>
            {
                await AboutMenu.HideAsync();
            };

            return button;
        }

        private async void OnFeedbackButtonClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();

            var linkLabel = new Label
            {
                Text = "点我进行反馈或投稿(将跳转到外部网页)",
                FontSize = 15,
                TextColor = Colors.DodgerBlue,
                TextDecorations = TextDecorations.Underline,
                HorizontalOptions = LayoutOptions.Center
            };

            var linkTap = new TapGestureRecognizer();
            linkTap.Tapped += async (s, args) =>
            {
                await FeedbackMenu.HideAsync();
                await Launcher.OpenAsync(new Uri("https://justindividual.site"));
            };
            linkLabel.GestureRecognizers.Add(linkTap);

            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 24,
                WidthRequest = 300,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "反馈 & 投稿",
                            FontSize = 21,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "软件遇到bug？没有自己的学校？",
                            FontSize = 15,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalTextAlignment = TextAlignment.Center
                        },
                        linkLabel,
                        CreateFeedbackCloseButton()
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: FeedbackMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.32
            );
        }

        private Button CreateFeedbackCloseButton()
        {
            var button = new Button
            {
                Text = "关闭",
                WidthRequest = 120,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };

            button.Clicked += async (s, e) =>
            {
                await FeedbackMenu.HideAsync();
            };

            return button;
        }

        private async void OnPrivacyPolicyClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();

            string policyText;
            using (var stream = await FileSystem.OpenAppPackageFileAsync("PrivacyPolicy.txt"))
            using (var reader = new StreamReader(stream))
            {
                policyText = await reader.ReadToEndAsync();
            }

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

            var confirmBtn = new Button
            {
                Text = "确认",
                WidthRequest = 120,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };
            confirmBtn.Clicked += async (s, args) =>
            {
                await PrivacyPolicyMenu.HideAsync();
            };

            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 24,
                WidthRequest = 340,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    Children =
                    {
                        new Label
                        {
                            Text = "隐私政策",
                            FontSize = 21,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        scrollView,
                        PopupWindow.CreateSeparator(),
                        confirmBtn
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: PrivacyPolicyMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.32
            );
        }

        private async void OnWidgetButtonClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();

            var messageLabel = new Label
            {
                Text = "该功能可在桌面添加小组件，提醒课程信息（由于安卓系统限制，课程信息可能会延迟数分钟更新）\n\n" +
                       "由于系统无法自动添加小组件，请按以下步骤手动添加：\n\n" +
                       "1. 返回手机桌面\n" +
                       "2. 长按桌面空白处\n" +
                       "3. 选择\"小组件\"或\"窗口小工具\"\n" +
                       "4. 找到\"课表助手\"\n" +
                       "5. 长按并拖动到桌面即可",
                FontSize = 14,
                LineBreakMode = LineBreakMode.WordWrap,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };

            var okBtn = new Button
            {
                Text = "我知道了",
                WidthRequest = 140,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };
            okBtn.Clicked += async (s, args) =>
            {
                await WidgetMenu.HideAsync();
            };

            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 24,
                WidthRequest = 300,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "桌面小组件",
                            FontSize = 21,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        messageLabel,
                        PopupWindow.CreateSeparator(),
                        okBtn
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: WidgetMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.32
            );
        }

        private async void OnAcknowledgmentsClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();

            string acknowledgmentsText;
            using (var stream = await FileSystem.OpenAppPackageFileAsync("Acknowledgments.txt"))
            using (var reader = new StreamReader(stream))
            {
                acknowledgmentsText = await reader.ReadToEndAsync();
            }

            var scrollView = new ScrollView
            {
                MaximumHeightRequest = 420,
                Content = new Label
                {
                    Text = acknowledgmentsText,
                    FontSize = 13,
                    LineBreakMode = LineBreakMode.WordWrap,
                    TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                }
            };

            var closeBtn = new Button
            {
                Text = "关闭",
                WidthRequest = 120,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };
            closeBtn.Clicked += async (s, args) =>
            {
                await AboutMenu.HideAsync();
            };

            var content = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 24,
                WidthRequest = 340,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    Children =
                    {
                        new Label
                        {
                            Text = "致谢名单",
                            FontSize = 21,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        scrollView,
                        PopupWindow.CreateSeparator(),
                        closeBtn
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: AboutMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.32
            );
        }


        private async void OnLuxInfoClicked(object sender, EventArgs e)
        {
            await CloseDialogsAsync();

            var okBtn = new Button
            {
                Text = "我知道了",
                WidthRequest = 140,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.Blue,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.Center
            };
            okBtn.Clicked += async (s, args) =>
            {
                await LuxInfoMenu.HideAsync();
            };

            var content = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 24,
                WidthRequest = 320,
                Content = new VerticalStackLayout
                {
                    Spacing = 14,
                    HorizontalOptions = LayoutOptions.Center,
                    Children =
                    {
                        new Label
                        {
                            Text = "什么是 Lux？",
                            FontSize = 21,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            HorizontalOptions = LayoutOptions.Center
                        },
                        new Label
                        {
                            Text = "Lux（勒克斯）是环境光照强度的国际单位。" +
                                   "手机通过光线传感器(不是摄像头，本软件不会调用摄像头)实时测量周围的光照值。\n\n" +
                                   "参考范围：\n" +
                                   "• 0~15 Lux：昏暗环境（如关灯的房间）\n" +
                                   "• 15~200 Lux：室内正常光线\n" +
                                   "• 200+ Lux：明亮环境\n\n" +
                                   "判定方法：\n" +
                                   "当 Lux 值低于 15 时，本软件将判定当前环境为\"昏暗\"，" +
                                   "并可能弹出护眼提示，建议开灯或调整屏幕亮度以保护视力。",
                            FontSize = 13,
                            LineBreakMode = LineBreakMode.WordWrap,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        },
                        PopupWindow.CreateSeparator(),
                        okBtn
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: LuxInfoMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.32
            );
        }

        private async Task CloseDialogsAsync()
        {
            if (AboutMenu.IsOpen)
            {
                await AboutMenu.HideAsync();
            }

            if (isolation.IsOpen)
            {
                await isolation.HideAsync();
            }

            if (EyeProtectionMenu.IsOpen)
            {
                await EyeProtectionMenu.HideAsync();
            }

            if (FeedbackMenu.IsOpen)
            {
                await FeedbackMenu.HideAsync();
            }

            if (PrivacyPolicyMenu.IsOpen)
            {
                await PrivacyPolicyMenu.HideAsync();
            }

            if (WidgetMenu.IsOpen)
            {
                await WidgetMenu.HideAsync();
            }

            if (LuxInfoMenu.IsOpen)
            {
                await LuxInfoMenu.HideAsync();
            }

            if (BgCropMenu.IsOpen)
            {
                await BgCropMenu.HideAsync();
            }

            if (OtherSettingsMenu.IsOpen)
            {
                await OtherSettingsMenu.HideAsync();
            }
        }

        private async Task ShowBgCropPopupAsync()
        {
            await CloseDialogsAsync();

            var imagePath = _configService.BackgroundImagePath;
            if (string.IsNullOrEmpty(imagePath) || !File.Exists(imagePath))
                return;

            var displayInfo = DeviceDisplay.MainDisplayInfo;
            var density = displayInfo.Density <= 0 ? 1.0 : displayInfo.Density;
            double screenWidth = displayInfo.Width / density;
            double screenHeight = displayInfo.Height / density;
            double screenAspect = screenWidth / screenHeight;

            double popupWidth = Math.Min(screenWidth - 32, 360);
            double popupHeight = Math.Min(screenHeight - 40, 620);
            double cropBoxW = popupWidth - 36;
            double cropBoxH = cropBoxW / screenAspect;
            double maxCropHeight = popupHeight * 0.30;
            if (cropBoxH > maxCropHeight)
            {
                cropBoxH = maxCropHeight;
                cropBoxW = cropBoxH * screenAspect;
            }

            double imgWidth = 1;
            double imgHeight = 1;
            try
            {
                using var stream = File.OpenRead(imagePath);
                var platformImage = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
                imgWidth = platformImage.Width;
                imgHeight = platformImage.Height;
            }
            catch
            {
                imgWidth = 1;
                imgHeight = 1;
            }

            double currentScale = Math.Clamp(_configService.BackgroundImageScale, 0.1, 5.0);
            double offsetLimitX = 0;
            double offsetLimitY = 0;

            var previewImage = new Image
            {
                Source = ImageSource.FromFile(imagePath),
                WidthRequest = cropBoxW,
                HeightRequest = cropBoxH,
                Aspect = Aspect.AspectFit,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center
            };

            void UpdatePreviewBounds(bool keepCurrentOffset)
            {
                var baseScale = Math.Min(cropBoxW / imgWidth, cropBoxH / imgHeight);
                var renderedWidth = imgWidth * baseScale * currentScale;
                var renderedHeight = imgHeight * baseScale * currentScale;
                offsetLimitX = Math.Abs(renderedWidth - cropBoxW) / 2;
                offsetLimitY = Math.Abs(renderedHeight - cropBoxH) / 2;

                previewImage.WidthRequest = renderedWidth;
                previewImage.HeightRequest = renderedHeight;

                if (!keepCurrentOffset)
                {
                    previewImage.TranslationX = offsetLimitX <= 0 ? 0 : _configService.BackgroundImageOffsetX * offsetLimitX;
                    previewImage.TranslationY = offsetLimitY <= 0 ? 0 : _configService.BackgroundImageOffsetY * offsetLimitY;
                    return;
                }

                previewImage.TranslationX = Clamp(previewImage.TranslationX, -offsetLimitX, offsetLimitX);
                previewImage.TranslationY = Clamp(previewImage.TranslationY, -offsetLimitY, offsetLimitY);
            }

            UpdatePreviewBounds(keepCurrentOffset: false);

            var cropContainer = new Grid
            {
                WidthRequest = cropBoxW,
                HeightRequest = cropBoxH,
                IsClippedToBounds = true,
                HorizontalOptions = LayoutOptions.Center,
                VerticalOptions = LayoutOptions.Center,
                Children = { previewImage }
            };

            const double viewportOutlineThickness = 2;

            var previewCard = new Border
            {
                BackgroundColor = Colors.Black.WithAlpha(0.08f),
                Padding = 8,
                StrokeThickness = 0,
                StrokeShape = new RoundRectangle { CornerRadius = 24 },
                HorizontalOptions = LayoutOptions.Center,
                Content = new Grid
                {
                    WidthRequest = cropBoxW + viewportOutlineThickness * 2,
                    HeightRequest = cropBoxH + viewportOutlineThickness * 2,
                    Children =
                    {
                        cropContainer,
                        new Border
                        {
                            BackgroundColor = Colors.Transparent,
                            Stroke = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                            StrokeThickness = viewportOutlineThickness,
                            StrokeShape = new Rectangle(),
                            WidthRequest = cropBoxW + viewportOutlineThickness * 2,
                            HeightRequest = cropBoxH + viewportOutlineThickness * 2,
                            HorizontalOptions = LayoutOptions.Center,
                            VerticalOptions = LayoutOptions.Center,
                            InputTransparent = true
                        }
                    }
                }
            };

            var panGesture = new PanGestureRecognizer();
            double panStartX = 0;
            double panStartY = 0;
            panGesture.PanUpdated += (s, args) =>
            {
                switch (args.StatusType)
                {
                    case GestureStatus.Started:
                        panStartX = previewImage.TranslationX;
                        panStartY = previewImage.TranslationY;
                        break;
                    case GestureStatus.Running:
                        previewImage.TranslationX = Clamp(panStartX + args.TotalX, -offsetLimitX, offsetLimitX);
                        previewImage.TranslationY = Clamp(panStartY + args.TotalY, -offsetLimitY, offsetLimitY);
                        break;
                }
            };
            cropContainer.GestureRecognizers.Add(panGesture);

            var zoomLabel = new Label
            {
                Text = $"缩放 {currentScale * 100:F0}%",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                HorizontalOptions = LayoutOptions.End
            };

            bool isUpdatingZoomControls = false;
            var zoomEntry = new Entry
            {
                Text = $"{currentScale * 100:F0}",
                Keyboard = Keyboard.Numeric,
                HorizontalTextAlignment = TextAlignment.Center,
                ClearButtonVisibility = ClearButtonVisibility.WhileEditing,
                WidthRequest = 88,
                MaxLength = 3,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgSec"] ?? Colors.White,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
            };

            void SetZoomScale(double newScale, bool keepCurrentOffset)
            {
                currentScale = Math.Clamp(newScale, 0, 5.0);
                isUpdatingZoomControls = true;
                zoomLabel.Text = $"缩放 {currentScale * 100:F0}%";
                zoomEntry.Text = $"{currentScale * 100:F0}";
                isUpdatingZoomControls = false;
                UpdatePreviewBounds(keepCurrentOffset);
            }

            var zoomSlider = new Slider
            {
                Minimum = 0,
                Maximum = 5,
                Value = currentScale,
                MinimumTrackColor = (Color?)Application.Current?.Resources["TextAccent"] ?? Colors.Blue,
                MaximumTrackColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                ThumbColor = (Color?)Application.Current?.Resources["TextAccent"] ?? Colors.Blue
            };
            zoomSlider.ValueChanged += (s, args) =>
            {
                if (isUpdatingZoomControls)
                    return;

                SetZoomScale(args.NewValue, keepCurrentOffset: true);
            };

            zoomEntry.TextChanged += (s, args) =>
            {
                if (isUpdatingZoomControls)
                    return;

                if (!double.TryParse(args.NewTextValue, out var percent))
                    return;

                var scale = percent / 100d;
                zoomSlider.Value = scale;
                SetZoomScale(scale, keepCurrentOffset: true);
            };

            zoomEntry.Unfocused += (s, args) =>
            {
                if (double.TryParse(zoomEntry.Text, out var percent))
                {
                    var scale = percent / 100d;
                    SetZoomScale(scale, keepCurrentOffset: true);
                }
                else
                {
                    SetZoomScale(currentScale, keepCurrentOffset: true);
                }
            };

            var zoomInputRow = new HorizontalStackLayout
            {
                Spacing = 8,
                HorizontalOptions = LayoutOptions.End,
                Children =
                {
                    new Label
                    {
                        Text = "填入缩放值",
                        FontSize = 13,
                        TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                        VerticalTextAlignment = TextAlignment.Center
                    },
                    zoomEntry,
                    new Label
                    {
                        Text = "%",
                        FontSize = 13,
                        TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                        VerticalTextAlignment = TextAlignment.Center
                    }
                }
            };

            var cancelBtn = new Button
            {
                Text = "取消",
                FontSize = 15,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgSec"] ?? Colors.LightGray,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                CornerRadius = 12,
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill,
                Shadow = null
            };
            cancelBtn.Clicked += async (s, args) => await BgCropMenu.HideAsync();

            var confirmBtn = new Button
            {
                Text = "确认",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.LightGray,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                CornerRadius = 12,
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill,
                Shadow = null
            };

            confirmBtn.Clicked += async (s, args) =>
            {
                _configService.BackgroundImageScale = currentScale;
                _configService.BackgroundImageOffsetX = offsetLimitX <= 0 ? 0 : previewImage.TranslationX / offsetLimitX;
                _configService.BackgroundImageOffsetY = offsetLimitY <= 0 ? 0 : previewImage.TranslationY / offsetLimitY;
                await BgCropMenu.HideAsync();
            };

            var hintLabel = new Label
            {
                Text = "导入后会先完整显示整张图片。拖动图片可调整位置，超出预览框的部分会自动裁切，缩放范围 10% 到 500%。",
                FontSize = 12,
                TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                HorizontalTextAlignment = TextAlignment.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            var actionGrid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Star)
                },
                ColumnSpacing = 12
            };
            Grid.SetColumn(cancelBtn, 0);
            Grid.SetColumn(confirmBtn, 1);
            actionGrid.Children.Add(cancelBtn);
            actionGrid.Children.Add(confirmBtn);

            var body = new VerticalStackLayout
            {
                Spacing = 10,
                Children =
                {
                    new Label
                    {
                        Text = "裁剪背景图",
                        FontSize = 20,
                        FontAttributes = FontAttributes.Bold,
                        TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                    },
                    previewCard,
                    zoomLabel,
                    zoomInputRow,
                    zoomSlider,
                    hintLabel,
                    actionGrid
                }
            };

            var content = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 18 },
                BackgroundColor = (Color?)Application.Current?.Resources["CardBg"] ?? Colors.White,
                Padding = 22,
                WidthRequest = popupWidth,
                MaximumHeightRequest = popupHeight,
                Content = body
            };

            await PopupWindow.ShowCustomAsync(
                host: BgCropMenu,
                content: content,
                animationMode: MenuAnimationMode.PopUp,
                showOverlay: true,
                horizontalAlign: LayoutOptions.Center,
                verticalAlign: LayoutOptions.Center,
                margin: new Thickness(0),
                overlayOpacity: 0.36
            );
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
