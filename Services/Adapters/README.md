# 课表导入适配器开发指南

## 📖 概述

本项目使用**适配器模式**支持不同学校的课表导入功能。每个学校需要一个独立的适配器来处理其特定的教务系统。

## 🏗️ 架构说明

```
用户点击"导入课表"
    ↓
LoginPage (登录界面)
    ↓
ParserService (解析服务)
    ↓
ISchoolAdapter (适配器接口)
    ↓
XmumAdapter / YourSchoolAdapter (具体实现)
    ↓
提取数据 → 解析 → 保存到数据库
```

## 🚀 快速开始

### 1️⃣ 复制模板

```bash
Services/Adapters/SchoolAdapterTemplate.cs
    → 复制并重命名为 →
Services/Adapters/YourSchoolAdapter.cs
```

### 2️⃣ 填写基本信息

打开您的适配器文件,修改以下内容:

```csharp
public class YourSchoolAdapter : ISchoolAdapter
{
    public string SchoolName => "YOUR_SCHOOL";  // 学校简称
    public string LoginUrl => "https://...";     // 登录页URL
    public string ScheduleUrl => "https://...";  // 课表页URL
}
```

### 3️⃣ 实现页面判断

```csharp
public bool IsLoginSuccess(string url)
{
    // 检查URL是否包含登录成功后的关键词
    return url.ToLower().Contains("student") || url.ToLower().Contains("home");
}

public bool IsSchedulePage(string url)
{
    // 检查是否在课表页面
    return url.ToLower().Contains("schedule") || url.ToLower().Contains("课表");
}
```

### 4️⃣ 编写JavaScript提取脚本

在浏览器中打开学校课表页面:

1. 按 `F12` 打开开发者工具
2. 在 Console 中测试选择器:

```javascript
// 测试能否获取课程行
var rows = document.querySelectorAll('table tr');
console.log(rows.length);  // 查看有多少行

// 测试能否获取课程名
var firstCourseName = rows[1].cells[0].innerText;
console.log(firstCourseName);
```

3. 编写完整的提取脚本:

```csharp
public string GetExtractScript()
{
    return @"(function() {
        var rows = document.querySelectorAll('table.schedule tr');
        var result = '';
        for(var i = 1; i < rows.length; i++) {
            var cells = rows[i].cells;
            if(cells.length >= 4) {
                var name = cells[0].innerText.trim();
                var time = cells[1].innerText.trim();
                var teacher = cells[2].innerText.trim();
                var location = cells[3].innerText.trim();
                if(name && time) {
                    result += name + '##' + time + '##' + teacher + '##' + location + '||';
                }
            }
        }
        return result || 'no_data';
    })()";
}
```

### 5️⃣ 实现数据解析

```csharp
public List<Course> ParseRawString(string rawData)
{
    var list = new List<Course>();
    if (string.IsNullOrEmpty(rawData) || rawData == "no_data")
        return list;

    var weekOrder = new Dictionary<string, int> {
        { "Monday", 1 }, { "Tuesday", 2 }, { "Wednesday", 3 },
        { "Thursday", 4 }, { "Friday", 5 }, { "Saturday", 6 }, { "Sunday", 7 }
    };

    var entries = rawData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

    foreach (var entry in entries)
    {
        var parts = entry.Split(new[] { "##" }, StringSplitOptions.None);
        if (parts.Length < 4) continue;

        string courseName = parts[0].Trim();
        string rawTimeContent = parts[1].Trim();
        string teacher = parts[2].Trim();
        string location = parts[3].Trim();

        // 使用正则表达式解析时间
        // 示例: "Monday 10.00am-12.00pm (A4#111)"
        var match = Regex.Match(rawTimeContent, 
            @"([a-zA-Z]+)\s+([\d.]+(?:am|pm))-([\d.]+(?:am|pm))\s*\(([^)]+)\)", 
            RegexOptions.IgnoreCase);

        if (match.Success)
        {
            list.Add(new Course
            {
                Name = courseName,
                Teacher = teacher,
                Location = match.Groups[4].Value.Trim(),
                DayOfWeek = match.Groups[1].Value.Trim(),
                StartTime = match.Groups[2].Value.ToLower().Trim(),
                EndTime = match.Groups[3].Value.ToLower().Trim(),
                HexColor = "#A2D2FF",
                IsManual = false
            });
        }
    }

    return list.OrderBy(c => weekOrder.ContainsKey(c.DayOfWeek) ? weekOrder[c.DayOfWeek] : 9).ToList();
}
```

### 6️⃣ 注册适配器

打开 `MauiProgram.cs`,找到适配器注册部分:

```csharp
// 将这行:
builder.Services.AddSingleton<ISchoolAdapter, XmumAdapter>();

// 改为:
builder.Services.AddSingleton<ISchoolAdapter, YourSchoolAdapter>();
```

**注意**: 同一时间只能注册一个适配器!

### 7️⃣ 编译和测试

1. 编译项目确保没有错误
2. 运行应用
3. 点击"导入课表"按钮
4. 测试登录和数据导入功能

## 📁 相关文件

| 文件路径 | 说明 |
|---------|------|
| `Services/Adapters/SchoolAdapterTemplate.cs` | **开发模板** - 包含详细注释和示例 |
| `Services/Adapters/ISchoolAdapter.cs` | 接口定义 - 所有适配器必须实现 |
| `Services/Adapters/XmumAdapter.cs` | XMUM学校的实现 - 可作为参考 |
| `Services/Adapters/FriendSchoolAdapter.cs` | 示例实现 - 可作为参考 |
| `Services/ParserService.cs` | 解析服务 - 调用适配器的入口 |
| `Views/LoginPage.xaml.cs` | 登录页面 - 使用适配器导入数据 |
| `MauiProgram.cs` | 依赖注入配置 - 注册适配器 |

## 🧪 测试清单

完成开发后,请按以下清单测试:

- [ ] 编译项目无错误
- [ ] JavaScript脚本能在浏览器Console中正确提取数据
- [ ] 登录判断逻辑正确 (IsLoginSuccess)
- [ ] 课表页判断逻辑正确 (IsSchedulePage)
- [ ] 数据提取格式正确 (GetExtractScript)
- [ ] 数据解析正确 (ParseRawString)
- [ ] 课程按星期正确排序
- [ ] 时间格式正确显示
- [ ] 异常情况处理正确 (无数据、网络错误等)

## 📝 数据格式约定

### JavaScript返回格式

```
课程名##时间##教师##地点||课程名##时间##教师##地点||...
```

**示例**:
```
数据结构##Monday 10.00am-12.00pm (A4#201)##张教授##A4#201||算法设计##Tuesday 2.00pm-4.00pm (B3#305)##李教授##B3#305||
```

### 时间字符串格式

```
星期 开始时间-结束时间 (地点)
```

**示例**:
- `Monday 10.00am-12.00pm (A4#111)`
- `Tuesday 2.00pm-4.00pm (Online)`
- `Wednesday 8.00am-10.00am (B3#205)`

## 🔍 调试技巧

### 1. 在浏览器中测试JavaScript

```javascript
// 在课表页面的Console中运行您的脚本
(function() {
    var rows = document.querySelectorAll('table tr');
    console.log('找到行数:', rows.length);

    for(var i = 0; i < Math.min(3, rows.length); i++) {
        var cells = rows[i].cells;
        console.log('第' + i + '行单元格数:', cells ? cells.length : 0);
        if(cells && cells.length > 0) {
            console.log('第一个单元格内容:', cells[0].innerText);
        }
    }
})()
```

### 2. 在应用中查看日志

在您的适配器中添加调试输出:

```csharp
public List<Course> ParseRawString(string rawData)
{
    System.Diagnostics.Debug.WriteLine($"[YourSchool] 原始数据: {rawData}");

    // ... 解析逻辑 ...

    System.Diagnostics.Debug.WriteLine($"[YourSchool] 解析出 {list.Count} 门课程");
    return list;
}
```

## ❓ 常见问题

### Q: 为什么我的适配器没有生效?

A: 检查 `MauiProgram.cs` 中是否正确注册了您的适配器,并且类名拼写正确。

### Q: JavaScript脚本返回 "no_data"?

A: 
1. 检查选择器是否正确 (在浏览器Console中测试)
2. 确认页面已完全加载
3. 检查表格结构是否与预期一致

### Q: 解析后没有课程显示?

A: 
1. 检查正则表达式是否匹配时间格式
2. 在 `ParseRawString` 中添加调试输出查看原始数据
3. 确认字段分割符 (## 和 ||) 正确

### Q: 可以同时支持多个学校吗?

A: 当前架构每次只能注册一个适配器。如需支持多校切换,需要:
1. 添加学校选择配置
2. 修改依赖注入逻辑支持动态选择
3. 在UI中添加学校选择功能

## 📚 参考资源

- **SchoolAdapterTemplate.cs**: 最详细的开发文档和代码示例
- **XmumAdapter.cs**: 真实的适配器实现,可直接参考
- **ISchoolAdapter.cs**: 接口定义和快速开始指南

## 💡 最佳实践

1. **先在浏览器测试**: 确保JavaScript脚本能正确提取数据后再写入代码
2. **使用正则表达式**: 灵活解析各种时间格式
3. **容错处理**: 对于无法解析的数据,跳过而不是抛出异常
4. **添加日志**: 使用 `Debug.WriteLine` 输出关键信息便于调试
5. **参考现有实现**: XmumAdapter 和 FriendSchoolAdapter 是很好的参考

## 📮 需要帮助?

如有疑问,请:
1. 查看 `SchoolAdapterTemplate.cs` 中的详细注释
2. 参考 `XmumAdapter.cs` 的实际实现
3. 检查 `ISchoolAdapter.cs` 中的快速开始指南

---

**祝您开发顺利! 🎉**
