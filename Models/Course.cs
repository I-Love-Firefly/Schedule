namespace Schedule2._0.Models
{
    public class Course
    {
        [SQLite.PrimaryKey, SQLite.AutoIncrement]
        public int Id { get; set; }
        public string Name { get; set; }
        public string Location { get; set; }
        public string Teacher { get; set; } 
        public string HexColor { get; set; }
        public bool IsManual { get; set; }
        public string StartTime { get; set; } // 例如 "10.00am"
        public string EndTime { get; set; }   // 例如 "12.00pm"
        public string DayOfWeek { get; set; } // 例如 "Monday"
        public bool IsDayVisible { get; set; }
    }
}