namespace Schedule2._0.Services
{
    public class ConfigService
    {
        // 定义存储的键名（Key）
        private const string ThemeKey = "user_theme_preference";
        private const string ThemeTagKey = "user_theme_tag_preference"; // 新增：标签存储键
        private const string LastUpdateKey = "last_sync_time";
        private const string PrivacyAcceptedKey = "privacy_policy_accepted";

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
        /// 上次同步课表的时间
        /// </summary>
        public DateTime LastSyncTime
        {
            get => Preferences.Default.Get(LastUpdateKey, DateTime.MinValue);
            set => Preferences.Default.Set(LastUpdateKey, value);
        }
    }
}