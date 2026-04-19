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

            await Task.Yield();
            await Task.Delay(50);
        }

        protected override void OnDisappearing()
        {
            base.OnDisappearing();
            LightSensorService.Instance.Stop();
        }

        private async void OnBackButtonClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("..");
        }

        protected override bool OnBackButtonPressed()
        {
            if (AboutMenu.IsOpen || isolation.IsOpen || EyeProtectionMenu.IsOpen || FeedbackMenu.IsOpen || PrivacyPolicyMenu.IsOpen)
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

            if (targetTheme.NeedsConfirmation)
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

            // 构建"爱上雷神?"确认对话框内容
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
                            Text = "保护眼睛",
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
                TextColor = Colors.White,
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
                Text = "",          //"点我进行反馈或投稿(该项目前不可用)",
                FontSize = 15,
                TextColor = Colors.DodgerBlue,
                TextDecorations = TextDecorations.Underline,
                HorizontalOptions = LayoutOptions.Center
            };

            //var linkTap = new TapGestureRecognizer();
            //linkTap.Tapped += async (s, args) =>
            //{
            //    await FeedbackMenu.HideAsync();
            //    await Launcher.OpenAsync(new Uri("http://42.193.179.91:5000"));
            //};
            //linkLabel.GestureRecognizers.Add(linkTap);

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
                            Text = "该项正在开发中，请通过邮箱进行反馈\nzyj2808868088@gmail.com",   //"软件遇到bug？没有自己的学校？",
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
                TextColor = Colors.White,
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
                TextColor = Colors.White,
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
                TextColor = Colors.White,
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
        }
    }
}
