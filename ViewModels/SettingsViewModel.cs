using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Schedule2._0.Services;
using Schedule2._0.Models;
using System.Collections.Generic;
using System.Linq;

namespace Schedule2._0.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        // ============================================================
        // 核心配置：将主题名、显示名、标签在这里统一管理
        // ============================================================
        private static readonly List<ThemeConfig> ThemePool = new()
        {
            new ThemeConfig { ResourceKey = "System", DisplayName = "跟随系统", Tag = ThemeTag.Light, Mode = 0 },
            new ThemeConfig { ResourceKey = "LightTheme", DisplayName = "浅色模式", Tag = ThemeTag.Light, Mode = 1 },
            new ThemeConfig { ResourceKey = "DarkTheme", DisplayName = "深色模式", Tag = ThemeTag.Dark, Mode = 2 },
            new ThemeConfig { ResourceKey = "PinkTheme", DisplayName = "猛男粉", Tag = ThemeTag.Light, Mode = 3 },
            new ThemeConfig { ResourceKey = "FireflyTheme", DisplayName = "流萤青", Tag = ThemeTag.Light, Mode = 4 },
            new ThemeConfig { ResourceKey = "WhiteEreCringeTheme", DisplayName = "爱上雷神!", Tag = ThemeTag.Light, Mode = 5, NeedsConfirmation = true }
        };

        private readonly ConfigService _configService;
        private readonly ThemeService _themeService;

        [ObservableProperty]
        private bool isFollowSystem;

        [ObservableProperty]
        private string currentThemeName;

        /// <summary>
        /// 当前主题标签属性（供 Page 拦截逻辑或 UI 反查调用）
        /// </summary>
        public string CurrentThemeTag
        {
            get
            {
                // 如果是跟随系统，实时询问内存中的视觉状态
                if (_configService.AppTheme == 0)
                {
                    return Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";
                }
                // 否则返回存储在本地的标签
                return _configService.CurrentTag;
            }
        }

        public SettingsViewModel(ConfigService configService, ThemeService themeService)
        {
            _configService = configService;
            _themeService = themeService;

            // 初始化开关状态
            IsFollowSystem = _configService.AppTheme == 0;

            // 初始化显示名称
            UpdateThemeDisplayOnly(_configService.AppTheme);
        }

        /// <summary>
        /// 监听开关切换
        /// </summary>
        partial void OnIsFollowSystemChanged(bool value)
        {
            // 如果已经是该状态，则不重复触发切换逻辑
            if (value && _configService.AppTheme != 0)
            {
                SwitchTheme("System");
            }
            else if (!value && _configService.AppTheme == 0)
            {
                SwitchTheme("LightTheme"); // 关闭跟随系统时默认回到浅色
            }
        }

        [RelayCommand]
        private void SwitchTheme(string themeMode)
        {
            // 根据主题 Key 找到对应的模型
            var themeInfo = ThemePool.FirstOrDefault(t => t.ResourceKey == themeMode);
            if (themeInfo == null) return;

            int modeValue = themeInfo.Mode;

            // --- 核心同步逻辑 1：先更新 Tag 标签 ---
            // 必须在 ApplySystemTheme 之前执行，否则状态栏颜色会慢半拍
            if (modeValue != 0)
            {
                _configService.CurrentTag = themeInfo.Tag.ToString().ToLower();
            }
            else
            {
                // 跟随系统时，预判当前的系统标签
                var systemTag = Application.Current.RequestedTheme == AppTheme.Dark ? "dark" : "light";
                _configService.CurrentTag = systemTag;
            }

            // --- 核心同步逻辑 2：更新内部开关状态 ---
            // 使用私有变量赋值，避免触发 OnIsFollowSystemChanged 导致递归
            isFollowSystem = (modeValue == 0);
            OnPropertyChanged(nameof(IsFollowSystem));

            // 执行主题切换应用
            UpdateTheme(modeValue, themeInfo.DisplayName);
        }

        /// <summary>
        /// 供 View 层（SettingsPage）根据 Key 获取模型（用于 NeedsConfirmation 判定）
        /// </summary>
        public ThemeConfig GetThemeByKey(string key)
        {
            return ThemePool.FirstOrDefault(t => t.ResourceKey == key);
        }

        private void UpdateTheme(int modeValue, string displayName)
        {
            // 1. 持久化存储主题索引
            _configService.AppTheme = modeValue;

            // 2. 更新 ViewModel 显示文本
            CurrentThemeName = displayName;

            // 3. 核心：触发 App.xaml.cs 中的全局主题切换
            // 同时也触发了 App.xaml.cs 里的 ThemeHelper.SyncStatusBar
            if (Application.Current is App app)
            {
                app.ApplySystemTheme(modeValue);
            }

            // 4. 通知属性已改变
            OnPropertyChanged(nameof(CurrentThemeTag));
        }

        /// <summary>
        /// 构造函数专用：只更新文字不触发现持久化逻辑
        /// </summary>
        private void UpdateThemeDisplayOnly(int modeValue)
        {
            // 寻找匹配 Mode 的主题，找不到则默认浅色
            var theme = ThemePool.FirstOrDefault(t => t.Mode == modeValue) ?? ThemePool[1];
            CurrentThemeName = theme.DisplayName;
        }
    }
}