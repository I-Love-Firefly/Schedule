using Schedule2._0.ViewModels;

namespace Schedule2._0.Views;

public partial class AddCoursePage : ContentPage
{
    // 通过构造函数注入 ViewModel
    public AddCoursePage(AddCourseViewModel viewModel)
    {
        InitializeComponent();

        // 将页面逻辑与 ViewModel 绑定
        BindingContext = viewModel;
    }
}