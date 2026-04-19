using Schedule2._0.Models;
using Schedule2._0.Services.Adapters;

namespace Schedule2._0.Services
{
    /// <summary>
    /// 课程解析服务 - 通过适配器模式支持多个学校的课表导入
    /// </summary>
    /// <remarks>
    /// 【架构说明】
    /// 此服务使用适配器模式,通过依赖注入获取特定学校的适配器,
    /// 从而实现对不同学校教务系统的统一处理。
    /// 
    /// 【工作流程】
    /// 1. MauiProgram.cs 中注册具体的学校适配器 (如 XmumAdapter)
    /// 2. 应用启动时,ParserService 自动获取注册的适配器
    /// 3. LoginPage 调用此服务获取JavaScript脚本和解析数据
    /// 4. 适配器负责具体的数据提取和解析逻辑
    /// 
    /// 【添加新学校支持】
    /// 1. 参考 Services/Adapters/SchoolAdapterTemplate.cs 创建新适配器
    /// 2. 实现 ISchoolAdapter 接口的所有方法
    /// 3. 在 MauiProgram.cs 中注册新适配器
    /// 4. 无需修改此文件的任何代码
    /// </remarks>
    public class ParserService
    {
        private readonly ISchoolAdapter _adapter;

        /// <summary>
        /// 构造函数注入: 程序启动时,MauiProgram 会自动把注册好的适配器传进来
        /// </summary>
        /// <param name="adapter">在 MauiProgram.cs 中注册的学校适配器实例</param>
        public ParserService(ISchoolAdapter adapter)
        {
            _adapter = adapter;
        }

        /// <summary>
        /// 获取用于在WebView中执行的JavaScript脚本
        /// </summary>
        /// <returns>适配器提供的JavaScript脚本字符串</returns>
        /// <remarks>
        /// 此脚本会在课表页面加载完成后执行,从HTML中提取课程数据
        /// </remarks>
        public string GetExtractScript() => _adapter.GetExtractScript();

        /// <summary>
        /// 解析从JavaScript脚本提取的原始数据字符串
        /// </summary>
        /// <param name="rawData">JavaScript返回的原始数据</param>
        /// <returns>解析后的课程列表</returns>
        /// <remarks>
        /// 将格式化的字符串数据转换为 Course 对象列表
        /// </remarks>
        public List<Course> ParseRawString(string rawData) => _adapter.ParseRawString(rawData);
    }
}