using Schedule2._0.Helpers;

namespace Schedule2._0.Services
{
    public class ConfigService
    {
        // 定义存储的键名（Key）
        private const string ThemeKey = "user_theme_preference";
        private const string ThemeTagKey = "user_theme_tag_preference"; // 新增：标签存储键
        private const string LastUpdateKey = "last_sync_time";
        private const string LegacyPrivacyAcceptedKey = "privacy_policy_accepted";
        private const string PrivacyAcceptedKey = "privacy_policy_accepted_v2_api";
        private const string Version21UpdateNotesSeenKey = "update_notes_seen_2_1_custom_dialog";
        private const string CardOpacityKey = "card_opacity";
        private const string BgImagePathKey = "background_image_path";
        private const string BgImageScaleKey = "background_image_scale";
        private const string BgImageOffsetXKey = "background_image_offset_x";
        private const string BgImageOffsetYKey = "background_image_offset_y";
        private const string WidgetBgColorKey = "widget_bg_color";
        private const string WidgetBgOpacityKey = "widget_bg_opacity";
        private const string CloudRecognitionEndpointKey = "cloud_schedule_recognition_endpoint";
        private const string DefaultCloudRecognitionEndpoint = "https://justindividual.site/schedule-ai";


        /// <summary>
        /// 用户主题设置：0-跟随系统, 1-浅色, 2-深色 ...
        /// </summary>
        public int AppTheme
        {
            get => Preferences.Default.Get(ThemeKey, 0); // 默认值为 0
            set => Preferences.Default.Set(ThemeKey, value);
        }

        /// <summary>
        /// 用户当前主题的标签属性：light 或 dark
        /// 用于在不读取数据库的情况下快速判断主题属性
        /// </summary>
        public string CurrentTag
        {
            get => Preferences.Default.Get(ThemeTagKey, "light"); // 默认浅色标签
            set => Preferences.Default.Set(ThemeTagKey, value);
        }

        /// <summary>
        /// 用户是否已同意隐私政策
        /// </summary>
        public bool PrivacyPolicyAccepted
        {
            get => Preferences.Default.Get(PrivacyAcceptedKey, false);
            set => Preferences.Default.Set(PrivacyAcceptedKey, value);
        }

        /// <summary>
        /// 2.0 用户同意隐私政策后会留下旧键；全新安装的 2.1 不会有该记录。
        /// </summary>
        public bool WasVersion20User => Preferences.Default.Get(LegacyPrivacyAcceptedKey, false);

        public bool Version21UpdateNotesSeen
        {
            get => Preferences.Default.Get(Version21UpdateNotesSeenKey, false);
            set => Preferences.Default.Set(Version21UpdateNotesSeenKey, value);
        }

        public bool ShouldShowVersion21UpdateNotes =>
            UpdateNoticePolicy.ShouldShow(WasVersion20User, Version21UpdateNotesSeen);

        /// <summary>
        /// 课程卡片透明度：0.0（完全透明）到 1.0（完全不透明）
        /// </summary>
        public double CardOpacity
        {
            get => Preferences.Default.Get(CardOpacityKey, 1.0);
            set => Preferences.Default.Set(CardOpacityKey, value);
        }

        /// <summary>
        /// 用户选择的背景图片路径（空字符串表示无背景图）
        /// </summary>
        public string BackgroundImagePath
        {
            get => Preferences.Default.Get(BgImagePathKey, string.Empty);
            set => Preferences.Default.Set(BgImagePathKey, value);
        }

        public double BackgroundImageScale
        {
            get => Preferences.Default.Get(BgImageScaleKey, 1.0);
            set => Preferences.Default.Set(BgImageScaleKey, value);
        }

        public double BackgroundImageOffsetX
        {
            get => Preferences.Default.Get(BgImageOffsetXKey, 0.0);
            set => Preferences.Default.Set(BgImageOffsetXKey, value);
        }

        public double BackgroundImageOffsetY
        {
            get => Preferences.Default.Get(BgImageOffsetYKey, 0.0);
            set => Preferences.Default.Set(BgImageOffsetYKey, value);
        }

        /// <summary>
        /// 上次同步课表的时间
        /// </summary>
        public DateTime LastSyncTime
        {
            get => Preferences.Default.Get(LastUpdateKey, DateTime.MinValue);
            set => Preferences.Default.Set(LastUpdateKey, value);
        }

      

        public Color? WidgetBgColor
        {
            get
            {
                var hex = Preferences.Default.Get(WidgetBgColorKey, "#FFFFFFFF");
                return Color.FromArgb(hex);
            }
            set
            {
                if (value != null)
                    Preferences.Default.Set(WidgetBgColorKey, value.ToArgbHex());
            }
        }

        public double WidgetBgOpacity
        {
            get => Preferences.Default.Get(WidgetBgOpacityKey, 1.0);
            set => Preferences.Default.Set(WidgetBgOpacityKey, value);
        }

        public string CloudRecognitionEndpoint
        {
            get => Preferences.Default.Get(CloudRecognitionEndpointKey, DefaultCloudRecognitionEndpoint);
            set => Preferences.Default.Set(CloudRecognitionEndpointKey, value.TrimEnd('/'));
        }

        /// <summary>
        /// 判断当前用户是否为VIP
        /// </summary>
        public bool IsVIP()
        {
            // 这里可根据实际业务逻辑判断
            // 例如：return Preferences.Default.Get("is_vip", false);
            // 目前默认返回false，可根据需要修改
            return Preferences.Default.Get("is_vip", true);
        }
    }
}
