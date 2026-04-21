using Microsoft.Maui.Controls;
using Schedule2._0.Services.Adapters;

namespace Schedule2._0.Views;

// 使用 QueryProperty 接收从 MainPage 传过来的 "school" 参数
[QueryProperty(nameof(SchoolType), "school")]
public partial class LoginPage : ContentPage
{
    private readonly Services.DatabaseService _db;
    private readonly ISchoolAdapterProvider _adapterProvider;

    // 当前正在使用的适配器（动态决定）
    private ISchoolAdapter _currentAdapter;

    // 接收路由参数的属性
    public string SchoolType { get; set; }

    public LoginPage(Services.DatabaseService db, ISchoolAdapterProvider adapterProvider)
    {
        InitializeComponent();
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _adapterProvider = adapterProvider ?? throw new ArgumentNullException(nameof(adapterProvider));

        // 绑定导航事件
        ScheduleWebView.Navigated += OnWebViewNavigated;
    }

    /// <summary>
    /// 当页面导航完成（参数已传递）时触发
    /// </summary>
    protected override void OnNavigatedTo(NavigatedToEventArgs args)
    {
        base.OnNavigatedTo(args);

        _currentAdapter = _adapterProvider.GetBySchoolCode(SchoolType);

        if (_currentAdapter != null)
        {
            ScheduleWebView.Source = _currentAdapter.LoginUrl;
        }
    }

    private async void OnWebViewNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Result != WebNavigationResult.Success || _currentAdapter == null) return;

        string url = e.Url.ToLower();

        // 1. 登录成功判断：调用适配器的逻辑
        if (_currentAdapter.IsLoginSuccess(url))
        {
            ScheduleWebView.Source = _currentAdapter.ScheduleUrl;
            return;
        }

        // 2. 到达课表页判断：显示抓取按钮
        if (_currentAdapter.IsSchedulePage(url))
        {
            ConfirmBtn.IsVisible = true;
            ConfirmBtn.ZIndex = 1000;
        }
        else
        {
            ConfirmBtn.IsVisible = false;
        }
    }

    /// <summary>
    /// 用户点击"确认导入"按钮
    /// </summary>
    /// <remarks>
    /// 【导入流程】
    /// 1. 从当前适配器获取JavaScript提取脚本
    /// 2. 在WebView中执行脚本,提取课程数据
    /// 3. 使用适配器解析原始数据为Course对象
    /// 4. 保存到数据库
    /// 5. 返回主页面
    /// 
    /// 【适配器调用示例】
    /// - _currentAdapter.GetExtractScript() - 获取JavaScript脚本
    /// - _currentAdapter.ParseRawString(raw) - 解析原始数据
    /// 
    /// 【添加新学校支持】
    /// 无需修改此方法!只需:
    /// 1. 创建新的适配器类 (参考 SchoolAdapterTemplate.cs)
    /// 2. 在 OnNavigatedTo() 中添加对应的 if 分支
    /// 3. 或直接在 MauiProgram.cs 中注册为默认适配器
    /// </remarks>
    private async void OnConfirmClicked(object sender, EventArgs e)
    {
        if (_currentAdapter == null) return;

        try
        {
            // 第一步: 从适配器获取JavaScript数据提取脚本
            // 此脚本会从课表页面的HTML中提取课程信息
            string script = _currentAdapter.GetExtractScript();

            // 第二步: 在WebView中执行JavaScript脚本
            string raw = await ScheduleWebView.EvaluateJavaScriptAsync(script);

            if (string.IsNullOrEmpty(raw) || raw == "null")
            {
                await DisplayAlertAsync("提示", "未能获取到课表数据，请确保页面已完全加载", "确定");
                return;
            }

            // 第三步: 使用适配器解析原始数据字符串
            // 适配器会将 "课程名##时间##教师||..." 格式转换为 Course 对象列表
            var courses = _currentAdapter.ParseRawString(raw);

            // 第四步: 保存到数据库
            await _db.SaveCoursesAsync(courses);

            // 第五步: 提示用户并返回主页
            await DisplayAlertAsync("成功", $"已同步 {courses.Count} 门课程", "确定");
            await Shell.Current.GoToAsync("..");
        }
        catch (Exception ex)
        {
            await DisplayAlertAsync("错误", $"同步失败: {ex.Message}", "确定");
        }
    }
}