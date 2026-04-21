using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;

namespace Schedule2._0.ViewModels
{
    // 必须是 partial 且继承 ObservableObject
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool isBusy;

        [ObservableProperty]
        private double cardOpacity = 1.0;

        public MainViewModel()
        {
            // 这里现在必须是空的，或者只初始化基础数据类型
        }
    }
}