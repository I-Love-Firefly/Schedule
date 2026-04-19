/*
 * ============================================================================
 * 学校适配器开发模板
 * ============================================================================
 * 
 * 【用途】
 * 当您需要为新学校添加课表导入功能时，复制此模板并按注释指引填写。
 * 
 * 【开发步骤】
 * 1. 复制此文件并重命名为 "学校英文名Adapter.cs" (如: StanfordAdapter.cs)
 * 2. 修改类名以匹配文件名 (如: public class StanfordAdapter : ISchoolAdapter)
 * 3. 填写学校基本信息 (SchoolName, LoginUrl, ScheduleUrl)
 * 4. 实现登录和课表页面判断逻辑 (IsLoginSuccess, IsSchedulePage)
 * 5. 编写JavaScript提取脚本 (GetExtractScript)
 * 6. 实现课程数据解析逻辑 (ParseRawString)
 * 7. 在 MauiProgram.cs 中注册您的适配器
 * 8. 测试并验证导入功能
 * 
 * 【参考示例】
 * 请参考 XmumAdapter.cs 和 FriendSchoolAdapter.cs 的实际实现
 * 
 * ============================================================================
 */

using System.Text.RegularExpressions;
using Schedule2._0.Models;

namespace Schedule2._0.Services.Adapters
{
    /// <summary>
    /// 【学校名称】适配器 - 用于从【学校教务系统名称】导入课表
    /// </summary>
    /// <remarks>
    /// 开发日期: YYYY-MM-DD
    /// 开发者: 您的名字
    /// 目标学校: 学校全称
    /// 教务系统: 系统名称/版本
    /// </remarks>
    public class SchoolAdapterTemplate : ISchoolAdapter
    {
        // ====================================================================
        // 第一部分: 学校基本信息配置
        // ====================================================================

        /// <summary>
        /// 学校简称 (用于显示和识别)
        /// </summary>
        /// <example>
        /// 示例: "XMUM", "PKU", "THU"
        /// </example>
        public string SchoolName => "TEMPLATE";

        /// <summary>
        /// 教务系统登录页面URL
        /// </summary>
        /// <remarks>
        /// 填写教务系统的登录入口地址
        /// 用户点击"导入课表"后会首先导航到此页面
        /// </remarks>
        /// <example>
        /// 示例: "https://jwc.yourschool.edu.cn/login"
        /// </example>
        public string LoginUrl => "https://example.edu.cn/login";

        /// <summary>
        /// 课表页面URL
        /// </summary>
        /// <remarks>
        /// 填写课表数据所在页面的地址
        /// 登录成功后系统会导航到此页面并执行数据提取
        /// </remarks>
        /// <example>
        /// 示例: "https://jwc.yourschool.edu.cn/student/schedule"
        /// </example>
        public string ScheduleUrl => "https://example.edu.cn/schedule";

        // ====================================================================
        // 第二部分: 页面状态判断逻辑
        // ====================================================================

        /// <summary>
        /// 判断是否登录成功
        /// </summary>
        /// <param name="url">当前WebView的URL</param>
        /// <returns>true表示已登录成功, false表示仍在登录页</returns>
        /// <remarks>
        /// 【实现思路】
        /// 方法1: 检查URL是否包含特定关键词 (如 "student", "home", "index")
        /// 方法2: 检查URL是否跳转到了学生主页
        /// 方法3: 检查URL参数 (如 ?loggedIn=true)
        /// 
        /// 【常见模式】
        /// - 登录前: https://example.edu.cn/login
        /// - 登录后: https://example.edu.cn/student/home
        /// 
        /// 【注意事项】
        /// - 使用 ToLower() 忽略大小写
        /// - 确保判断条件足够精确,避免误判
        /// </remarks>
        /// <example>
        /// // 示例1: 检查URL关键词
        /// return url.ToLower().Contains("student") || url.ToLower().Contains("home");
        /// 
        /// // 示例2: 检查完整路径
        /// return url.StartsWith("https://example.edu.cn/student/");
        /// 
        /// // 示例3: 排除登录页
        /// return !url.ToLower().Contains("login");
        /// </example>
        public bool IsLoginSuccess(string url)
        {
            // TODO: 根据您学校的实际情况实现登录判断逻辑
            // return url.ToLower().Contains("关键词");
            return false;
        }

        /// <summary>
        /// 判断当前是否在课表页面
        /// </summary>
        /// <param name="url">当前WebView的URL</param>
        /// <returns>true表示在课表页, false表示不在</returns>
        /// <remarks>
        /// 【实现思路】
        /// 方法1: 检查URL是否包含"schedule", "课表", "timetable"等关键词
        /// 方法2: 检查URL路径是否匹配课表页路径
        /// 方法3: 检查URL参数 (如 ?page=schedule)
        /// 
        /// 【注意事项】
        /// - 必须确保在正确的页面执行JavaScript提取脚本
        /// - 判断条件要精确,避免在错误页面执行脚本
        /// </remarks>
        /// <example>
        /// // 示例1: 检查URL包含关键词
        /// return url.ToLower().Contains("schedule") || url.ToLower().Contains("课表");
        /// 
        /// // 示例2: 检查完整URL
        /// return url == ScheduleUrl;
        /// 
        /// // 示例3: 检查路径
        /// return url.Contains("/student/schedule");
        /// </example>
        public bool IsSchedulePage(string url)
        {
            // TODO: 根据您学校的实际情况实现课表页判断逻辑
            // return url.ToLower().Contains("关键词");
            return false;
        }

        // ====================================================================
        // 第三部分: JavaScript数据提取脚本
        // ====================================================================

        /// <summary>
        /// 获取用于从网页提取课程数据的JavaScript脚本
        /// </summary>
        /// <returns>JavaScript脚本字符串</returns>
        /// <remarks>
        /// 【脚本功能】
        /// 该脚本会在课表页面中执行,从HTML表格中提取课程信息
        /// 
        /// 【开发步骤】
        /// 1. 在浏览器中打开学校课表页面
        /// 2. 按F12打开开发者工具
        /// 3. 在Console中测试选择器,找到课程数据的HTML元素
        /// 4. 编写JavaScript代码提取所需字段
        /// 5. 将多个字段用特殊分隔符连接(如 ## 和 ||)
        /// 
        /// 【数据格式约定】
        /// - 每门课程的字段用 "##" 分隔
        /// - 多门课程之间用 "||" 分隔
        /// - 建议格式: "课程名##时间##教师||课程名##时间##教师||..."
        /// 
        /// 【常见HTML结构】
        /// 
        /// 结构1: 标准表格
        /// <table>
        ///   <tr>
        ///     <td>课程名</td>
        ///     <td>时间</td>
        ///     <td>教师</td>
        ///     <td>地点</td>
        ///   </tr>
        /// </table>
        /// 
        /// 结构2: 带class的div
        /// <div class="course-list">
        ///   <div class="course-item">
        ///     <span class="name">课程名</span>
        ///     <span class="time">时间</span>
        ///   </div>
        /// </div>
        /// 
        /// 【JavaScript选择器示例】
        /// - document.querySelectorAll('table tr')  // 获取所有表格行
        /// - document.querySelectorAll('.course')   // 获取所有课程元素
        /// - cells[0].innerText.trim()              // 获取单元格文本并去空格
        /// 
        /// 【错误处理】
        /// - 如果没有数据,返回 'no_data'
        /// - 确保所有文本都使用 .trim() 去除空格
        /// - 检查元素是否存在再访问其属性
        /// </remarks>
        /// <example>
        /// // 示例1: 从标准表格提取
        /// return @"(function() {
        ///     var rows = document.querySelectorAll('table.schedule tr');
        ///     var result = '';
        ///     for(var i = 1; i < rows.length; i++) {  // 跳过表头
        ///         var cells = rows[i].cells;
        ///         if(cells.length >= 4) {
        ///             var name = cells[0].innerText.trim();      // 课程名
        ///             var time = cells[1].innerText.trim();      // 时间
        ///             var teacher = cells[2].innerText.trim();   // 教师
        ///             var location = cells[3].innerText.trim();  // 地点
        ///             if(name && time) {
        ///                 result += name + '##' + time + '##' + teacher + '##' + location + '||';
        ///             }
        ///         }
        ///     }
        ///     return result || 'no_data';
        /// })()";
        /// 
        /// // 示例2: 从div列表提取
        /// return @"(function() {
        ///     var courses = document.querySelectorAll('.course-item');
        ///     var result = '';
        ///     courses.forEach(function(course) {
        ///         var name = course.querySelector('.name')?.innerText.trim();
        ///         var time = course.querySelector('.time')?.innerText.trim();
        ///         if(name && time) {
        ///             result += name + '##' + time + '||';
        ///         }
        ///     });
        ///     return result || 'no_data';
        /// })()";
        /// </example>
        public string GetExtractScript()
        {
            // TODO: 编写JavaScript脚本从课表页面提取数据
            // 1. 在浏览器开发者工具中测试选择器
            // 2. 确定数据字段和分隔符格式
            // 3. 填写完整的JavaScript代码

            /*
            return @"(function() {
                // 第一步: 选择包含课程数据的元素
                var rows = document.querySelectorAll('选择器');
                var result = '';

                // 第二步: 遍历每个课程
                for(var i = 0; i < rows.length; i++) {
                    var cells = rows[i].cells;

                    // 第三步: 提取各个字段
                    var courseName = cells[索引].innerText.trim();
                    var time = cells[索引].innerText.trim();
                    var teacher = cells[索引].innerText.trim();
                    var location = cells[索引].innerText.trim();

                    // 第四步: 拼接数据
                    if(courseName && time) {
                        result += courseName + '##' + time + '##' + teacher + '##' + location + '||';
                    }
                }

                // 第五步: 返回结果
                return result || 'no_data';
            })()";
            */

            return "no_data";
        }

        // ====================================================================
        // 第四部分: 课程数据解析逻辑
        // ====================================================================

        /// <summary>
        /// 解析从JavaScript脚本提取的原始数据字符串
        /// </summary>
        /// <param name="rawData">JavaScript返回的原始数据字符串</param>
        /// <returns>解析后的课程列表</returns>
        /// <remarks>
        /// 【功能说明】
        /// 将JavaScript脚本提取的字符串数据解析为Course对象列表
        /// 
        /// 【输入格式】
        /// 从GetExtractScript()返回的数据,格式如:
        /// "课程1##时间1##教师1||课程2##时间2##教师2||..."
        /// 
        /// 【解析步骤】
        /// 1. 按 "||" 分割得到每门课程的数据
        /// 2. 对每门课程,按 "##" 分割得到各个字段
        /// 3. 使用正则表达式从时间字符串中提取:
        ///    - 星期 (Monday, Tuesday, ...)
        ///    - 开始时间 (如 10.00am)
        ///    - 结束时间 (如 12.00pm)
        ///    - 地点 (如 A4#111)
        /// 4. 创建Course对象并填充各个属性
        /// 5. 按星期排序并返回
        /// 
        /// 【时间格式示例】
        /// - "Monday 10.00am-12.00pm (A4#111)"
        /// - "Tuesday 2.00pm-4.00pm (B3#205)"
        /// - "Wednesday 8.00am-10.00am (Online)"
        /// 
        /// 【正则表达式模式】
        /// 基本模式: ([星期]) ([开始时间])-([结束时间]) \(([地点])\)
        /// 示例: @"([a-zA-Z]+)\s+([\d.]+(?:am|pm))-([\d.]+(?:am|pm))\s*\(([^)]+)\)"
        /// 
        /// 【Course对象属性】
        /// - Id: 主键,自动生成
        /// - Name: 课程名称
        /// - Location: 上课地点
        /// - Teacher: 授课教师
        /// - HexColor: 卡片背景色 (建议使用 "#A2D2FF")
        /// - IsManual: 是否手动添加 (导入的课程设为 false)
        /// - StartTime: 开始时间 (如 "10.00am")
        /// - EndTime: 结束时间 (如 "12.00pm")
        /// - DayOfWeek: 星期 (如 "Monday")
        /// - IsDayVisible: 是否显示星期标题 (由MainPage设置,此处不用管)
        /// 
        /// 【多时间段处理】
        /// 如果一门课程在多个时间段上课 (如周一和周三),需要:
        /// 1. 将时间字符串分割成多个段落
        /// 2. 为每个时间段创建单独的Course对象
        /// 3. 确保每个Course对象只包含一个时间段
        /// 
        /// 【排序规则】
        /// 使用星期顺序排序:
        /// Monday(1) -> Tuesday(2) -> Wednesday(3) -> Thursday(4) 
        /// -> Friday(5) -> Saturday(6) -> Sunday(7)
        /// 
        /// 【错误处理】
        /// - 如果rawData为空或"no_data",返回空列表
        /// - 如果字段数量不足,跳过该条数据
        /// - 如果正则匹配失败,尝试备用解析方法
        /// - 捕获所有异常并返回空列表
        /// </remarks>
        /// <example>
        /// // 解析流程示例:
        /// 
        /// // 输入数据:
        /// string rawData = "数据结构##Monday 10.00am-12.00pm (A4#201)##张教授||"
        ///                + "算法设计##Tuesday 2.00pm-4.00pm (B3#305)##李教授||";
        /// 
        /// // 步骤1: 分割课程
        /// var entries = rawData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);
        /// // entries[0] = "数据结构##Monday 10.00am-12.00pm (A4#201)##张教授"
        /// // entries[1] = "算法设计##Tuesday 2.00pm-4.00pm (B3#305)##李教授"
        /// 
        /// // 步骤2: 分割字段
        /// foreach (var entry in entries) {
        ///     var parts = entry.Split(new[] { "##" }, StringSplitOptions.None);
        ///     string courseName = parts[0].Trim();     // "数据结构"
        ///     string timeContent = parts[1].Trim();    // "Monday 10.00am-12.00pm (A4#201)"
        ///     string teacher = parts[2].Trim();        // "张教授"
        ///     
        ///     // 步骤3: 正则匹配
        ///     var match = Regex.Match(timeContent, 
        ///         @"([a-zA-Z]+)\s+([\d.]+(?:am|pm))-([\d.]+(?:am|pm))\s*\(([^)]+)\)");
        ///     
        ///     if (match.Success) {
        ///         string day = match.Groups[1].Value;      // "Monday"
        ///         string start = match.Groups[2].Value;    // "10.00am"
        ///         string end = match.Groups[3].Value;      // "12.00pm"
        ///         string location = match.Groups[4].Value; // "A4#201"
        ///         
        ///         // 步骤4: 创建Course对象
        ///         var course = new Course {
        ///             Name = courseName,
        ///             Teacher = teacher,
        ///             Location = location,
        ///             DayOfWeek = day,
        ///             StartTime = start.ToLower(),
        ///             EndTime = end.ToLower(),
        ///             HexColor = "#A2D2FF",
        ///             IsManual = false
        ///         };
        ///         list.Add(course);
        ///     }
        /// }
        /// 
        /// // 步骤5: 排序
        /// return list.OrderBy(c => weekOrder[c.DayOfWeek]).ToList();
        /// </example>
        public List<Course> ParseRawString(string rawData)
        {
            /*
            var list = new List<Course>();

            // 第一步: 验证输入数据
            if (string.IsNullOrEmpty(rawData) || rawData == "no_data")
                return list;

            // 定义星期排序顺序
            var weekOrder = new Dictionary<string, int> {
                { "Monday", 1 }, { "Tuesday", 2 }, { "Wednesday", 3 },
                { "Thursday", 4 }, { "Friday", 5 }, { "Saturday", 6 }, { "Sunday", 7 }
            };

            // 第二步: 按分隔符分割课程
            var entries = rawData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                // 第三步: 分割字段
                var parts = entry.Split(new[] { "##" }, StringSplitOptions.None);
                if (parts.Length < 2) continue; // 至少需要课程名和时间

                string courseName = parts[0].Trim();
                string rawTimeContent = parts[1].Trim();
                string teacher = parts.Length > 2 ? parts[2].Trim() : "未知";

                // 第四步: 使用正则表达式解析时间信息
                // 模式: 星期 开始时间-结束时间 (地点)
                // 示例: Monday 10.00am-12.00pm (A4#111)
                var match = Regex.Match(rawTimeContent, 
                    @"([a-zA-Z]+)\s+([\d.]+(?:am|pm|AM|PM))-([\d.]+(?:am|pm|AM|PM))\s*\(([^)]+)\)", 
                    RegexOptions.IgnoreCase);

                if (match.Success)
                {
                    // 第五步: 创建Course对象
                    list.Add(new Course
                    {
                        Name = courseName,
                        Teacher = teacher,
                        HexColor = "#A2D2FF",              // 默认蓝色
                        IsManual = false,                   // 导入的课程标记为非手动
                        DayOfWeek = match.Groups[1].Value.Trim(),
                        StartTime = match.Groups[2].Value.ToLower().Trim(),
                        EndTime = match.Groups[3].Value.ToLower().Trim(),
                        Location = match.Groups[4].Value.Trim()
                    });
                }
                else
                {
                    // 备用解析逻辑: 处理非标准格式
                    // TODO: 根据实际情况实现备用解析
                }
            }

            // 第六步: 按星期排序
            return list.OrderBy(c => weekOrder.ContainsKey(c.DayOfWeek) ? weekOrder[c.DayOfWeek] : 9).ToList();
            */

            // TODO: 取消注释上面的代码并根据您的数据格式调整
            return new List<Course>();
        }
    }
}

/*
 * ============================================================================
 * 注册适配器到依赖注入容器
 * ============================================================================
 * 
 * 完成适配器开发后,需要在 MauiProgram.cs 中注册:
 * 
 * 1. 找到 MauiProgram.cs 文件
 * 2. 在 CreateMauiApp() 方法中找到服务注册部分
 * 3. 将默认适配器改为您的新适配器:
 * 
 * // 原来 (使用XMUM适配器):
 * builder.Services.AddSingleton<ISchoolAdapter, XmumAdapter>();
 * 
 * // 修改为 (使用您的适配器):
 * builder.Services.AddSingleton<ISchoolAdapter, YourSchoolAdapter>();
 * 
 * 注意: 同一时间只能注册一个适配器!
 * 
 * ============================================================================
 * 测试清单
 * ============================================================================
 * 
 * 开发完成后,请按以下步骤测试:
 * 
 * ☐ 1. 编译项目确保没有语法错误
 * ☐ 2. 在浏览器中验证JavaScript脚本能正确提取数据
 * ☐ 3. 测试登录功能 (IsLoginSuccess判断是否准确)
 * ☐ 4. 测试页面跳转 (IsSchedulePage判断是否准确)
 * ☐ 5. 测试数据提取 (GetExtractScript是否返回正确格式)
 * ☐ 6. 测试数据解析 (ParseRawString是否正确创建Course对象)
 * ☐ 7. 测试课程显示 (课程是否按星期正确排序)
 * ☐ 8. 测试异常情况 (无数据、网络错误等)
 * 
 * ============================================================================
 */
