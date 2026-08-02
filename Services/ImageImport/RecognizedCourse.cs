using Schedule2._0.Models;

namespace Schedule2._0.Services.ImageImport;

public sealed class RecognizedCourse
{
    public string Name { get; set; } = "";
    public string Teacher { get; set; } = "";
    public string Location { get; set; } = "";
    public string DayOfWeek { get; set; } = "";
    public string StartTime { get; set; } = "";
    public string EndTime { get; set; } = "";
    public double Confidence { get; set; }
    public string SourceText { get; set; } = "";

    public bool IsComplete =>
        !string.IsNullOrWhiteSpace(Name) &&
        !string.IsNullOrWhiteSpace(DayOfWeek) &&
        !string.IsNullOrWhiteSpace(StartTime) &&
        !string.IsNullOrWhiteSpace(EndTime);

    public Course ToCourse() => new()
    {
        Name = Name.Trim(),
        Teacher = Teacher.Trim(),
        Location = Location.Trim(),
        DayOfWeek = DayOfWeek.Trim(),
        StartTime = StartTime.Trim(),
        EndTime = EndTime.Trim(),
        HexColor = "#A2D2FF",
        IsManual = false
    };
}

public sealed class ScheduleImageParseResult
{
    public List<RecognizedCourse> Courses { get; init; } = [];
    public List<string> Warnings { get; init; } = [];
}
