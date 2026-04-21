using Schedule2._0.Helpers;
using Schedule2._0.Models;
using Schedule2._0.Services;
using Schedule2._0.ViewModels;
using Microsoft.Maui.Controls.Shapes;
#if ANDROID
using Microsoft.Maui.Controls;
using Schedule2._0.Services;
#endif

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

            // 控制“其他设置”板块显示
            var otherSettingsSection = this.FindByName<Microsoft.Maui.Controls.Layout>("OtherSettingsSection");
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
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                VerticalOptions = LayoutOptions.Center,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
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

            var opacityRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.Center
            };
            var opacityTitle = new Label { Text = "卡片不透明度", FontSize = 15, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black };
            Grid.SetColumn(opacityTitle, 0);
            Grid.SetColumn(opacitySlider, 1);
            Grid.SetColumn(opacityLabel, 2);
            opacityRow.Children.Add(opacityTitle);
            opacityRow.Children.Add(opacitySlider);
            opacityRow.Children.Add(opacityLabel);

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
                            Text = "其他设置(以后会陆续添加新功能)",
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        },
                        bgItem,
                        opacityRow,
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

        private async Task ShowCardOpacityPopupAsync()
        {
            // 课程卡片不透明度（直接可调节）
            var opacityLabel = new Label
            {
                Text = $"{(int)(_configService.CardOpacity * 100)}%",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color?)Application.Current?.Resources["TextSec"] ?? Colors.Gray,
                VerticalOptions = LayoutOptions.Center,
                HorizontalTextAlignment = TextAlignment.End,
                HorizontalOptions = LayoutOptions.EndAndExpand,
                WidthRequest = 45
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
                HorizontalOptions = LayoutOptions.Fill,
                WidthRequest = 120
            };
            opacitySlider.ValueChanged += (s, args) =>
            {
                var value = Math.Round(args.NewValue, 2);
                _configService.CardOpacity = value;
                opacityLabel.Text = $"{(int)(value * 100)}%";
            };

            var opacityRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition(GridLength.Auto),
                    new ColumnDefinition(GridLength.Star),
                    new ColumnDefinition(GridLength.Auto)
                },
                ColumnSpacing = 10,
                VerticalOptions = LayoutOptions.Center
            };
            var opacityTitle = new Label { Text = "卡片不透明度", FontSize = 15, FontAttributes = FontAttributes.Bold, VerticalOptions = LayoutOptions.Center, TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black };
            Grid.SetColumn(opacityTitle, 0);
            Grid.SetColumn(opacitySlider, 1);
            Grid.SetColumn(opacityLabel, 2);
            opacityRow.Children.Add(opacityTitle);
            opacityRow.Children.Add(opacitySlider);
            opacityRow.Children.Add(opacityLabel);

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
            closeBtn.Clicked += async (s, args) => await CardOpacityMenu.HideAsync();

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
                            Text = "卡片不透明度",
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        },
                        opacityRow,
                        closeBtn
                    }
                }
            };

            await PopupWindow.ShowCustomAsync(
                host: CardOpacityMenu,
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
                Text = "点我进行反馈或投稿(该项目前不可用)",
                FontSize = 15,
                TextColor = Colors.DodgerBlue,
                TextDecorations = TextDecorations.Underline,
                HorizontalOptions = LayoutOptions.Center
            };

            var linkTap = new TapGestureRecognizer();
            linkTap.Tapped += async (s, args) =>
            {
                await FeedbackMenu.HideAsync();
                await Launcher.OpenAsync(new Uri("http://42.193.179.91:5000"));
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

        private async void OnAcknowledgmentsClicked(object sender, EventArgs e)
        {
            if (sender is VisualElement view)
            {
                await view.ScaleToAsync(0.98, 60);
                await view.ScaleToAsync(1.0, 60);
            }

            await CloseDialogsAsync();

            string ackText;
            try
            {
                using var stream = await FileSystem.OpenAppPackageFileAsync("Acknowledgments.txt");
                using var reader = new StreamReader(stream);
                ackText = await reader.ReadToEndAsync();
            }
            catch
            {
                ackText = "无法加载致谢名单。";
            }

            var scrollView = new ScrollView
            {
                MaximumHeightRequest = 420,
                Content = new Label
                {
                    Text = ackText,
                    FontSize = 13,
                    LineBreakMode = LineBreakMode.WordWrap,
                    TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                }
            };

            var closeBtn = new Button
            {
                Text = "关闭",
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                BackgroundColor = (Color?)Application.Current?.Resources["BtnBgMain"] ?? Colors.LightGray,
                TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black,
                CornerRadius = 12,
                HeightRequest = 44,
                HorizontalOptions = LayoutOptions.Fill
            };
            closeBtn.Clicked += async (s, args) => await AboutMenu.HideAsync();

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
                            Text = "致谢名单 ( ゜- ゜)つロ",
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold,
                            HorizontalOptions = LayoutOptions.Center,
                            TextColor = (Color?)Application.Current?.Resources["TextMain"] ?? Colors.Black
                        },
                        scrollView,
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
                overlayOpacity: 0.36
            );
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
        }

        // 修正签名，支持 TapGestureRecognizer
        private async void OnLuxInfoClicked(object sender, TappedEventArgs e)
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

        // 恢复标准实现：关闭所有弹窗
        private async Task CloseDialogsAsync()
        {
            if (AboutMenu.IsOpen)
                await AboutMenu.HideAsync();
            if (isolation.IsOpen)
                await isolation.HideAsync();
            if (EyeProtectionMenu.IsOpen)
                await EyeProtectionMenu.HideAsync();
            if (FeedbackMenu.IsOpen)
                await FeedbackMenu.HideAsync();
            if (PrivacyPolicyMenu.IsOpen)
                await PrivacyPolicyMenu.HideAsync();
            if (WidgetMenu.IsOpen)
                await WidgetMenu.HideAsync();
            if (LuxInfoMenu.IsOpen)
                await LuxInfoMenu.HideAsync();
            if (BgCropMenu.IsOpen)
                await BgCropMenu.HideAsync();
            if (OtherSettingsMenu.IsOpen)
                await OtherSettingsMenu.HideAsync();
        }

        private async Task ShowBgCropPopupAsync()
        {
            // TODO: 实现背景裁剪弹窗逻辑
            await Task.CompletedTask;
        }
    }
}
