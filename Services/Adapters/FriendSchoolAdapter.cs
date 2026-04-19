using Schedule2._0.Models;

namespace Schedule2._0.Services.Adapters
{
    public class FriendSchoolAdapter : ISchoolAdapter
    {
        public string SchoolName => "Friend School";
        public string LoginUrl => "https://example.com/login"; // 暂时填个占位符
        public string ScheduleUrl => "https://example.com/schedule";

        public bool IsLoginSuccess(string url) => url.Contains("success");
        public bool IsSchedulePage(string url) => url.Contains("timetable");

        public string GetExtractScript() => "return 'no_data';";

        public List<Course> ParseRawString(string rawData) => new List<Course>();
    }
}