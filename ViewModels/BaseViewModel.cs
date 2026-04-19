using CommunityToolkit.Mvvm.ComponentModel;

namespace Schedule2._0.ViewModels
{
    // ObservableObject 是 CommunityToolkit.Mvvm 提供的魔法
    // 它能自动处理属性更改通知，让 UI 实时更新
    public partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private string title; // 每个页面的标题

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;  // 标记是否正在抓取课表或加载数据

        // 计算属性：当 isBusy 改变时，这会自动通知 UI
        public bool IsNotBusy => !isBusy;
    }
}