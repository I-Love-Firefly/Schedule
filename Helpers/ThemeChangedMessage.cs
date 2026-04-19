using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Schedule2._0.Models // 请根据你实际的文件夹路径调整命名空间
{
    /// <summary>
    /// 主题变更消息：当用户切换主题（包括粉色、流萤等）时，发送此信号
    /// </summary>
    public class ThemeChangedMessage : ValueChangedMessage<string>
    {
        public ThemeChangedMessage(string value) : base(value)
        {
        }
    }
}