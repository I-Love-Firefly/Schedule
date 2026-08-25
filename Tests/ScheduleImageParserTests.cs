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

    [Fact]
    public void ParsesGenericEnglishGridUsingAxisAndFieldOrder()
    {
        var document = new OcrDocument
        {
            ImageWidth = 2048,
            ImageHeight = 1010,
            HorizontalLines = [36, 112, 188, 263, 340, 401, 463, 552, 629, 705],
            VerticalLines = [161, 608, 882, 1234, 1366, 1807, 1938],
            Regions =
            [
                R("Monday", 350, 10, 420, 30), R("Tuesday", 700, 10, 780, 30),
                R("Wednesday", 1010, 10, 1100, 30), R("Thursday", 1260, 10, 1340, 30),
                R("Friday", 1540, 10, 1620, 30), R("Saturday", 1830, 10, 1910, 30),
                R("Sunday", 1960, 10, 2030, 30),
                R("8.00am-9.00am", 15, 60, 145, 85), R("9.00am-10.00am", 15, 135, 145, 160),
                R("10.00am-11.00am", 10, 210, 150, 235), R("11.00am-12.00pm", 10, 285, 150, 310),
                R("12.00pm-1.00pm", 10, 360, 150, 385), R("1.00pm-2.00pm", 10, 420, 150, 445),
                R("2.00pm-3.00pm", 10, 490, 150, 515), R("3.00pm-4.00pm", 10, 575, 150, 600),
                R("4.00pm-5.00pm", 10, 650, 150, 675),
                R("MPU4.1", 350, 200, 420, 220), R("Community Service", 310, 230, 470, 250),
                R("Maryani Binti Ahmad", 300, 260, 480, 280), R("A4#111", 350, 290, 420, 310),
                R("(Week 1-14)", 330, 315, 450, 335),
                R("SOF203", 350, 410, 420, 430), R("Fundamentals of Network Technology", 260, 440, 520, 460),
                R("Tharsiniy A/P Ramasamy", 290, 475, 490, 495), R("A3#620", 350, 510, 420, 530),
                R("(Week 1-14)", 330, 532, 450, 548),
                R("SOF108", 700, 562, 770, 580), R("Computer Architecture", 660, 590, 820, 610),
                R("Ng Tiong Sik", 690, 620, 790, 640), R("A3#602", 700, 650, 770, 670),
                R("(Week 1-14)", 680, 680, 800, 698),
                R("SOF105", 1020, 45, 1090, 65), R("Data Structures", 990, 75, 1120, 95),
                R("Nooraziera Akmal Binti Sukor", 940, 105, 1170, 125), R("A4#G01", 1020, 140, 1090, 160),
                R("(Week 1-14)", 1000, 165, 1120, 182),
                R("MPU3322", 1020, 475, 1100, 495), R("Integrity and Anti-Corruption", 960, 505, 1160, 525),
                R("Loh Yoong Keong", 990, 535, 1130, 555), R("A2#G01", 1020, 570, 1090, 590),
                R("(Week 1-14)", 1000, 600, 1120, 620),
                R("SOF107", 1560, 80, 1630, 100), R("Introduction of Software Engineering", 1450, 110, 1740, 130),
                R("Al-Fawareh Hejab Ma'azer Khaled", 1440, 145, 1740, 165), R("A3#602", 1560, 180, 1630, 200),
                R("(Week 1-14)", 1540, 210, 1660, 230)
            ]
        };

        var courses = new ScheduleImageParser().Parse(document).Courses;

        Assert.Equal(6, courses.Count);
        var community = Assert.Single(courses, x => x.Name == "Community Service");
        Assert.Equal("Maryani Binti Ahmad", community.Teacher);
        Assert.Equal("A4#111", community.Location);
        Assert.Equal("10.00am", community.StartTime);
        Assert.Equal("12.00pm", community.EndTime);
        var software = Assert.Single(courses, x => x.Name == "Introduction of Software Engineering");
        Assert.Equal("08.00am", software.StartTime);
        Assert.Equal("11.00am", software.EndTime);
    }

    [Fact]
    public void ParsesSingleCharacterChineseHeadersAndDomesticFields()
    {
        var document = new OcrDocument
        {
            ImageWidth = 700,
            ImageHeight = 600,
            VerticalLines = [100, 200, 300, 400, 500, 600],
            HorizontalLines = [50, 100, 200, 300, 400, 500],
            Regions =
            [
                R("一", 135, 60, 165, 85), R("二", 235, 60, 265, 85),
                R("三", 335, 60, 365, 85), R("四", 435, 60, 465, 85),
                R("五", 535, 60, 565, 85),
                R("上1[08:00-]", 0, 115, 95, 140), R("上2", 20, 155, 70, 180),
                R("数据结构", 215, 115, 275, 138), R("张三", 280, 115, 315, 138),
                R("1-16周", 215, 145, 270, 168), R("第1-2节", 275, 145, 335, 168),
                R("博学207", 215, 175, 280, 195)
            ]
        };

        var course = Assert.Single(new ScheduleImageParser().Parse(document).Courses);

        Assert.Equal("Tuesday", course.DayOfWeek);
        Assert.Equal("数据结构", course.Name);
        Assert.Equal("张三", course.Teacher);
        Assert.Equal("博学207", course.Location);
        Assert.Equal("08.00am", course.StartTime);
        Assert.Equal("09.40am", course.EndTime);
    }

    [Fact]
    public void ExcludesPeriodAxisWhenSundayIsFirstColumn()
    {
        var document = new OcrDocument
        {
            ImageWidth = 900,
            ImageHeight = 600,
            VerticalLines = [100, 200, 300, 400, 500, 600, 700, 800],
            HorizontalLines = [50, 100, 200, 300, 400, 500],
            Regions =
            [
                R("星期日", 125, 60, 175, 85), R("星期一", 225, 60, 275, 85),
                R("星期二", 325, 60, 375, 85), R("星期三", 425, 60, 475, 85),
                R("星期四", 525, 60, 575, 85), R("星期五", 625, 60, 675, 85),
                R("星期六", 725, 60, 775, 85),
                R("上1[08:00-]", 0, 115, 98, 140), R("上2", 20, 155, 70, 180),
                R("美国文学", 715, 115, 780, 138), R("陈惠", 715, 145, 755, 168),
                R("第1-2节", 715, 175, 775, 198), R("奉贤3教楼113", 705, 205, 795, 228)
            ]
        };

        var course = Assert.Single(new ScheduleImageParser().Parse(document).Courses);

        Assert.Equal("Saturday", course.DayOfWeek);
        Assert.Equal("美国文学", course.Name);
        Assert.DoesNotContain("上1", course.Name);
    }

    [Fact]
    public void BlocksWritingWhenThreeCoursesCollapseIntoOneDay()
    {
        var document = BuildFiveDayQualityDocument(
            ("Course A", 430, 120),
            ("Course B", 430, 220),
            ("Course C", 430, 320));

        var result = new ScheduleImageParser().Parse(document);

        Assert.Equal(3, result.Courses.Count);
        Assert.False(result.IsWriteSafe);
    }

    [Fact]
    public void BlocksWritingWhenMostCoursesHaveNoTeacherOrLocation()
    {
        var document = BuildFiveDayQualityDocument(
            ("Course A", 230, 120),
            ("Course B", 430, 220),
            ("Course C", 630, 320));

        var result = new ScheduleImageParser().Parse(document);

        Assert.Equal(3, result.Courses.Count);
        Assert.False(result.IsWriteSafe);
    }

    private static OcrDocument BuildFiveDayQualityDocument(params (string Text, int X, int Y)[] courses)
    {
        var regions = new List<OcrTextRegion>
        {
            R("Monday", 210, 30, 290, 60), R("Tuesday", 410, 30, 490, 60),
            R("Wednesday", 610, 30, 690, 60), R("Thursday", 810, 30, 890, 60),
            R("Friday", 1010, 30, 1090, 60),
            R("08:00-09:40", 0, 110, 95, 140), R("10:00-11:40", 0, 210, 95, 240),
            R("14:00-15:40", 0, 310, 95, 340)
        };
        regions.AddRange(courses.Select(x => R(x.Text, x.X, x.Y, x.X + 80, x.Y + 30)));
        return new OcrDocument
        {
            ImageWidth = 1200,
            ImageHeight = 500,
            Regions = regions,
            VerticalLines = [100, 300, 500, 700, 900, 1100],
            HorizontalLines = [80, 180, 280, 380]
        };
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
