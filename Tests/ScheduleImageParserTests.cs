using Schedule2._0.Services.ImageImport;
using Xunit;

namespace Schedule2._0.Tests;

public sealed class ScheduleImageParserTests
{
    [Fact]
    public void ParsesChineseGridWithExplicitTimes()
    {
        var document = Document(
            R("周一", 80, 20, 140, 50), R("周二", 280, 20, 340, 50),
            R("高等数学", 75, 100, 150, 125), R("08:00-09:40", 75, 130, 160, 150),
            R("A101教室", 75, 155, 150, 178), R("张老师", 75, 182, 145, 204),
            R("大学英语", 275, 220, 350, 245), R("10：00～11：40", 275, 250, 370, 272));

        var result = new ScheduleImageParser().Parse(document);

        Assert.Equal(2, result.Courses.Count);
        var math = Assert.Single(result.Courses, x => x.Name == "高等数学");
        Assert.Equal("Monday", math.DayOfWeek);
        Assert.Equal("08.00am", math.StartTime);
        Assert.Equal("09.40am", math.EndTime);
        Assert.Equal("A101教室", math.Location);
        Assert.Equal("张老师", math.Teacher);
    }

    [Fact]
    public void ParsesPeriodRangeAndFlagsDefaultTimesForReview()
    {
        var document = Document(
            R("星期一", 80, 20, 140, 50), R("星期二", 280, 20, 340, 50),
            R("数据结构", 275, 100, 350, 125), R("第3-4节", 275, 130, 340, 152));

        var course = Assert.Single(new ScheduleImageParser().Parse(document).Courses);
        Assert.Equal("Tuesday", course.DayOfWeek);
        Assert.Equal("10.00am", course.StartTime);
        Assert.Equal("11.40am", course.EndTime);
        Assert.True(course.IsComplete);
    }

    [Fact]
    public void DoesNotInventTimeWhenScreenshotHasNoTimeOrPeriod()
    {
        var document = Document(
            R("周一", 80, 20, 140, 50), R("周二", 280, 20, 340, 50),
            R("操作系统", 75, 100, 150, 125));

        var result = new ScheduleImageParser().Parse(document);
        var course = Assert.Single(result.Courses);
        Assert.False(course.IsComplete);
        Assert.Contains(result.Warnings, x => x.Contains("缺少准确时间"));
    }

    private static OcrDocument Document(params OcrTextRegion[] regions) => new()
    {
        ImageWidth = 500,
        ImageHeight = 800,
        Regions = regions
    };

    private static OcrTextRegion R(string text, float left, float top, float right, float bottom) =>
        new(text, left, top, right, bottom);
}
