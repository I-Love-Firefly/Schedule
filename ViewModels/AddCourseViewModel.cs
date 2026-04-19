using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Schedule2._0.Models;
using Schedule2._0.Services;

namespace Schedule2._0.ViewModels
{
    public partial class AddCourseViewModel : ObservableObject
    {
        private readonly DatabaseService _dbService;

        [ObservableProperty] string courseName;
        [ObservableProperty] string day;
        [ObservableProperty] string location;
        [ObservableProperty] string startTime;
        [ObservableProperty] string endTime;
        [ObservableProperty] List<string> daysList;

        /*==========================================================================================================================================*/
        //此处添加新的属性，例如课程老师等
        //例如：[ObservableProperty] string teacherName;
        /*==========================================================================================================================================*/

        public AddCourseViewModel(DatabaseService dbService)
        {
            _dbService = dbService;
            DaysList = new List<string> { "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday" };
        }

        [RelayCommand]
        private async Task SaveAndExit() { if (await ProcessAndSave()) await Shell.Current.Navigation.PopAsync(); }

        [RelayCommand]
        private async Task SaveAndNext()
        {
            if (await ProcessAndSave())
            {
                CourseName = Day = Location = StartTime = EndTime = string.Empty;

                /*==========================================================================================================================================*/
                //此处在用户点击“保存并继续”后重置新的属性，例如课程老师等
                //例如：TeacherName = string.Empty;
                /*==========================================================================================================================================*/

                await Shell.Current.DisplayAlertAsync("成功", "已保存", "确定");
            }
        }

        private async Task<bool> ProcessAndSave()
        {
            var course = new Course { Name = CourseName, 
                                    DayOfWeek = Day, 
                                    Location = Location, 
                                    StartTime = StartTime, 
                                    EndTime = EndTime, 
                                    IsManual = true, 
                                    HexColor = "#A2D2FF",
                /*==========================================================================================================================================*/
                //以上为创建课程对象的基本属性，可在下面添加新的属性，例如课程老师等
                //例如：Teacher = TeacherName
                //注意：如果添加了新的属性，请确保在Models\Course.cs类中也添加对应的属性，并且在数据库中也有相应的字段，否则可能会导致保存失败或者数据不完整
                /*==========================================================================================================================================*/
            };
            await _dbService.SaveSingleCourseAsync(course);
            return true;
        }
    }
}