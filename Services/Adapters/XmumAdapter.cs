using Schedule2._0.Models; // 确保引用了模型

namespace Schedule2._0.Services.Adapters
{
    // 1. 必须改为 public 确保外部可访问
    // 2. 必须继承 : ISchoolAdapter 签署“合同”
    public class XmumAdapter : ISchoolAdapter
    {
        public string SchoolName => "XMUM";
        public string LoginUrl => "https://ac.xmu.edu.my/";
        public string ScheduleUrl => "https://ac.xmu.edu.my/student/index.php?c=Default&a=Wdkc";

        public bool IsLoginSuccess(string url) => url.ToLower().Contains("a=inf");
        public bool IsSchedulePage(string url) => url.ToLower().Contains("wdkc");

        public string GetExtractScript()
        {
            return @"(function() {
                var rows = document.querySelectorAll('table tr');
                var result = '';
                for(var i=1; i<rows.length; i++) {
                    var cells = rows[i].cells;
                    if(cells.length > 5) {
                        var name = cells[2].innerText.trim();
                        var time = cells[5].innerText.trim();
                        var teacher = cells[4].innerText.trim();
                        if(name && time) {
                            result += name + '##' + time + '##' + teacher + '||';
                        }
                    }
                }
                return result || 'no_data';
            })()";
        }

        public List<Course> ParseRawString(string rawData)
        {
            var list = new List<Course>();
            if (string.IsNullOrEmpty(rawData) || rawData == "no_data") return list;

            var weekOrder = new Dictionary<string, int> {
                { "Monday", 1 }, { "Tuesday", 2 }, { "Wednesday", 3 },
                { "Thursday", 4 }, { "Friday", 5 }, { "Saturday", 6 }, { "Sunday", 7 }
            };

            var entries = rawData.Split(new[] { "||" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var entry in entries)
            {
                var parts = entry.Split(new[] { "##" }, StringSplitOptions.None);
                if (parts.Length < 3) continue;

                string courseName = parts[0].Trim();
                string rawTimeContent = parts[1].Trim();
                string teacher = parts[2].Trim();

                var foundDays = weekOrder.Keys.Where(d => rawTimeContent.Contains(d)).ToList();
                var timeSegments = new List<string>();

                if (foundDays.Count > 1)
                {
                    var sortedFoundDays = weekOrder.Keys
                        .Where(d => rawTimeContent.Contains(d))
                        .Select(d => new { Day = d, Index = rawTimeContent.IndexOf(d) })
                        .OrderBy(x => x.Index)
                        .ToList();

                    for (int i = 0; i < sortedFoundDays.Count; i++)
                    {
                        int start = sortedFoundDays[i].Index;
                        int length = (i + 1 < sortedFoundDays.Count)
                                     ? sortedFoundDays[i + 1].Index - start
                                     : rawTimeContent.Length - start;

                        timeSegments.Add(rawTimeContent.Substring(start, length).Trim());
                    }
                }
                else
                {
                    timeSegments.Add(rawTimeContent);
                }

                foreach (var seg in timeSegments)
                {
                    string day = "Unknown";
                    foreach (var d in weekOrder.Keys) { if (seg.Contains(d)) { day = d; break; } }

                    if (day != "Unknown")
                    {
                        var timeSplit = seg.Split('(');
                        string timeOnly = timeSplit[0].Replace(day, "").Trim();
                        string location = seg.Contains(")") ? seg.Split(')')[0].Split('(')[1] : "Unknown";

                        // 拆分开始时间和结束时间 (如 "10.00am-12.00pm")
                        string startTime = timeOnly;
                        string endTime = "";
                        int dashIndex = timeOnly.IndexOf('-');
                        if (dashIndex >= 0)
                        {
                            startTime = timeOnly.Substring(0, dashIndex).Trim().ToLower();
                            endTime = timeOnly.Substring(dashIndex + 1).Trim().ToLower();
                        }

                        list.Add(new Course
                        {
                            Name = courseName,
                            Teacher = teacher,
                            StartTime = startTime,
                            EndTime = endTime,
                            Location = location,
                            DayOfWeek = day,
                            HexColor = "#A2D2FF",
                            IsManual = false
                        });
                    }
                }
            }
            return list.OrderBy(c => weekOrder.ContainsKey(c.DayOfWeek) ? weekOrder[c.DayOfWeek] : 9).ToList();
        }
    }
}