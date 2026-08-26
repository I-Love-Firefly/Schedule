using System.Collections.ObjectModel;
using System.Globalization;
using Schedule2._0.Services;
using Schedule2._0.Services.ImageImport;

namespace Schedule2._0.Views;

public partial class ScheduleImageImportPage : ContentPage
{
    private readonly IOcrService _ocrService;
    private readonly ScheduleRecognitionCoordinator _recognizer;
    private readonly DatabaseService _database;
    private readonly ObservableCollection<RecognizedCourse> _courses = [];
    private bool _writeAllowed;

    public ScheduleImageImportPage(
        IOcrService ocrService,
        ScheduleRecognitionCoordinator recognizer,
        DatabaseService database)
    {
        InitializeComponent();
        _ocrService = ocrService;
        _recognizer = recognizer;
        _database = database;
        CoursesView.ItemsSource = _courses;
    }

    private async void OnPickImageClicked(object sender, EventArgs e)
    {
        if (!_ocrService.IsSupported)
        {
            await DisplayAlertAsync("暂不支持", "课程表截图识别目前仅支持 Android。", "确定");
            return;
        }

        var file = await FilePicker.Default.PickAsync(new PickOptions
        {
            PickerTitle = "选择完整课程表截图",
            FileTypes = FilePickerFileType.Images
        });
        if (file is null) return;

        SetBusy(true, "正在通过 DeepSeek API 识别课程表……");
        var cachePath = Path.Combine(FileSystem.CacheDirectory, $"schedule-ocr-{Guid.NewGuid():N}{Path.GetExtension(file.FileName)}");
        try
        {
            await using (var input = await file.OpenReadAsync())
            await using (var output = File.Create(cachePath))
                await input.CopyToAsync(output);

            var document = await _ocrService.RecognizeAsync(cachePath);
            var result = await _recognizer.RecognizeAsync(cachePath, document);
            _courses.Clear();
            foreach (var course in result.Courses) _courses.Add(course);
            _writeAllowed = result.IsWriteSafe;

            var source = string.IsNullOrWhiteSpace(result.RecognitionSource) ? "" : $"{result.RecognitionSource}：";
            var summary = $"{source}识别到 {_courses.Count} 门课程。请逐项校对后再写入。";
            if (result.Warnings.Count > 0) summary += Environment.NewLine + string.Join(Environment.NewLine, result.Warnings);
            if (_courses.Count > 0 && !_writeAllowed)
                summary += Environment.NewLine + "本次结果未通过质量校验，请重新截图或改用手动导入。";
            StatusLabel.Text = summary;
            ReviewConfirmation.IsVisible = _courses.Count > 0 && !_writeAllowed;
            ReviewCheckBox.IsChecked = false;
            UpdateSaveButton();
        }
        catch (Exception ex)
        {
            _courses.Clear();
            _writeAllowed = false;
            SaveButton.IsEnabled = false;
            ReviewConfirmation.IsVisible = false;
            StatusLabel.Text = "API 识别失败，未写入任何课程。";
            await DisplayAlertAsync("API 识别失败", $"请检查网络或稍后重试。\n{ex.Message}", "确定");
        }
        finally
        {
            if (File.Exists(cachePath)) File.Delete(cachePath);
            SetBusy(false);
        }
    }

    private async void OnSaveClicked(object sender, EventArgs e)
    {
        foreach (var course in _courses)
        {
            if (!TryNormalizeDay(course.DayOfWeek, out var day) ||
                !TryNormalizeTime(course.StartTime, out var start) ||
                !TryNormalizeTime(course.EndTime, out var end))
            {
                await DisplayAlertAsync("格式有误", $"课程“{course.Name}”的星期或时间格式无法识别。星期可填写周一至周日；时间可填写 08:00 或 08.00am。", "确定");
                return;
            }
            course.DayOfWeek = day;
            course.StartTime = start;
            course.EndTime = end;
        }

        var invalid = _courses.Where(x => !x.IsComplete).ToList();
        if (invalid.Count > 0)
        {
            await DisplayAlertAsync("请先补全", $"还有 {invalid.Count} 门课程缺少名称、星期或起止时间。", "确定");
            return;
        }

        var confirmed = await DisplayAlertAsync(
            "写入课程表",
            $"将用识别出的 {_courses.Count} 门课程替换之前自动导入的课程；手动添加的课程会保留。是否继续？",
            "写入",
            "取消");
        if (!confirmed) return;

        await _database.SaveCoursesAsync(_courses.Select(x => x.ToCourse()).ToList());
        await DisplayAlertAsync("完成", $"已写入 {_courses.Count} 门课程。", "确定");
        await Shell.Current.GoToAsync("..");
    }

    private void OnDeleteCourseClicked(object sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: RecognizedCourse course })
            _courses.Remove(course);
        UpdateSaveButton();
        StatusLabel.Text = $"当前保留 {_courses.Count} 门课程，请校对后写入。";
    }

    private void OnReviewConfirmedChanged(object sender, CheckedChangedEventArgs e) => UpdateSaveButton();

    private void UpdateSaveButton() =>
        SaveButton.IsEnabled = _courses.Count > 0 && (_writeAllowed || ReviewCheckBox.IsChecked);

    private static bool TryNormalizeDay(string value, out string normalized)
    {
        normalized = "";
        var text = value.Trim().ToLowerInvariant().Replace("星期", "周").Replace("礼拜", "周");
        string[][] aliases =
        [
            ["monday", "mon", "周一", "1"], ["tuesday", "tue", "周二", "2"],
            ["wednesday", "wed", "周三", "3"], ["thursday", "thu", "周四", "4"],
            ["friday", "fri", "周五", "5"], ["saturday", "sat", "周六", "6"],
            ["sunday", "sun", "周日", "周天", "7", "0"]
        ];
        string[] days = ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Sunday"];
        for (var i = 0; i < aliases.Length; i++)
        {
            if (!aliases[i].Contains(text)) continue;
            normalized = days[i];
            return true;
        }
        return false;
    }

    private static bool TryNormalizeTime(string value, out string normalized)
    {
        normalized = "";
        var text = value.Trim().ToLowerInvariant().Replace('：', ':').Replace('.', ':');
        string[] formats = ["H:mm", "HH:mm", "h:mmtt", "hh:mmtt"];
        if (!DateTime.TryParseExact(text, formats, CultureInfo.InvariantCulture, DateTimeStyles.AllowWhiteSpaces, out var time))
            return false;
        normalized = time.ToString("hh.mmtt", CultureInfo.InvariantCulture).ToLowerInvariant();
        return true;
    }

    private void SetBusy(bool busy, string? message = null)
    {
        BusyIndicator.IsVisible = busy;
        BusyIndicator.IsRunning = busy;
        PickImageButton.IsEnabled = !busy;
        ReviewCheckBox.IsEnabled = !busy;
        SaveButton.IsEnabled = !busy && _courses.Count > 0 && (_writeAllowed || ReviewCheckBox.IsChecked);
        if (message is not null) StatusLabel.Text = message;
    }

}
