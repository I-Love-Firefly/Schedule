using Schedule2._0.Models;

namespace Schedule2._0.Services.Adapters
{
    /// <summary>
    /// 学校适配器接口 - 定义课表导入功能的统一规范
    /// </summary>
    /// <remarks>
    /// 【接口用途】
    /// 此接口定义了课表导入功能所需的所有方法和属性。
    /// 每个学校需要实现此接口来提供特定教务系统的数据提取逻辑。
    /// 
    /// 【实现步骤】
    /// 1. 复制 SchoolAdapterTemplate.cs 作为起点
    /// 2. 实现所有属性和方法 (见模板中的详细注释)
    /// 3. 在 MauiProgram.cs 中注册您的适配器
    /// 
    /// 【现有实现】
    /// - XmumAdapter.cs: 马来西亚厦门大学教务系统
    /// - FriendSchoolAdapter.cs: 示例学校教务系统
    /// - SchoolAdapterTemplate.cs: 开发模板 (仅供参考,不可直接使用)
    /// 
    /// 【注意事项】
    /// - 必须添加 public 修饰符,否则会出现可访问性错误
    /// - 所有方法都必须实现,不能留空
    /// - JavaScript脚本必须返回约定格式的数据字符串
    /// </remarks>
    public interface ISchoolAdapter
    {
        /// <summary>
        /// 学校简称 (用于日志和调试)
        /// </summary>
        /// <example>"XMUM", "PKU", "THU"</example>
        string SchoolName { get; }

        /// <summary>
        /// 教务系统登录页面URL
        /// </summary>
        /// <example>"https://jwc.yourschool.edu.cn/login"</example>
        string LoginUrl { get; }

        /// <summary>
        /// 课表数据页面URL
        /// </summary>
        /// <example>"https://jwc.yourschool.edu.cn/student/schedule"</example>
        string ScheduleUrl { get; }

        /// <summary>
        /// 判断用户是否登录成功
        /// </summary>
        /// <param name="url">当前WebView的URL</param>
        /// <returns>true表示已登录, false表示未登录</returns>
        /// <remarks>
        /// 通过检查URL变化判断登录状态,常见方法:
        /// - URL包含特定关键词 (如 "student", "home")
        /// - URL跳转到了非登录页
        /// </remarks>
        bool IsLoginSuccess(string url);

        /// <summary>
        /// 判断当前是否在课表页面
        /// </summary>
        /// <param name="url">当前WebView的URL</param>
        /// <returns>true表示在课表页, false表示不在</returns>
        /// <remarks>
        /// 用于确认在正确页面执行JavaScript脚本
        /// </remarks>
        bool IsSchedulePage(string url);

        /// <summary>
        /// 获取JavaScript数据提取脚本
        /// </summary>
        /// <returns>JavaScript脚本字符串</returns>
        /// <remarks>
        /// 此脚本会在WebView中执行,从HTML提取课程数据。
        /// 返回格式约定: "课程名##时间##教师||课程名##时间##教师||..."
        /// 详见 SchoolAdapterTemplate.cs 中的详细说明
        /// </remarks>
        string GetExtractScript();

        /// <summary>
        /// 解析JavaScript返回的原始数据
        /// </summary>
        /// <param name="rawData">JavaScript脚本返回的原始字符串</param>
        /// <returns>解析后的课程对象列表</returns>
        /// <remarks>
        /// 将字符串数据转换为 Course 对象列表,并按星期排序。
        /// 详见 SchoolAdapterTemplate.cs 中的详细实现示例
        /// </remarks>
        List<Course> ParseRawString(string rawData);
    }
}

/*
 * ============================================================================
 * 快速开始指南
 * ============================================================================
 * 
 * 要为新学校添加课表导入支持,请按以下步骤操作:
 * 
 * 1. 📋 复制模板
 *    - 复制 Services/Adapters/SchoolAdapterTemplate.cs
 *    - 重命名为 YourSchoolAdapter.cs
 * 
 * 2. ✏️ 填写信息
 *    - 修改类名
 *    - 填写 SchoolName, LoginUrl, ScheduleUrl
 *    - 实现 IsLoginSuccess() 和 IsSchedulePage()
 * 
 * 3. 🔧 开发脚本
 *    - 在浏览器中打开课表页面
 *    - 按F12,在Console中测试数据提取
 *    - 将JavaScript代码填入 GetExtractScript()
 * 
 * 4. 📊 解析数据
 *    - 根据数据格式实现 ParseRawString()
 *    - 使用正则表达式提取时间和地点信息
 *    - 创建并返回 Course 对象列表
 * 
 * 5. 🔌 注册适配器
 *    - 打开 MauiProgram.cs
 *    - 找到适配器注册部分
 *    - 改为: builder.Services.AddSingleton<ISchoolAdapter, YourSchoolAdapter>();
 * 
 * 6. ✅ 测试验证
 *    - 编译项目
 *    - 运行应用
 *    - 测试登录和导入功能
 * 
 * 详细说明请参考 SchoolAdapterTemplate.cs 中的完整注释!
 * 
 * ============================================================================
 */