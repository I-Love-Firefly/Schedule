using System.Collections;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Schedule2._0.Views;

public partial class InputBox : ContentView, INotifyPropertyChanged
{
    public static readonly BindableProperty TitleProperty = BindableProperty.Create(nameof(Title), typeof(string), typeof(InputBox), string.Empty);
    public static readonly BindableProperty TextProperty = BindableProperty.Create(nameof(Text), typeof(string), typeof(InputBox), string.Empty, BindingMode.TwoWay);
    public static readonly BindableProperty ItemsSourceProperty = BindableProperty.Create(nameof(ItemsSource), typeof(IList), typeof(InputBox), null);
    public static readonly BindableProperty IsTimeModeProperty = BindableProperty.Create(nameof(IsTimeMode), typeof(bool), typeof(InputBox), false);

    public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public IList ItemsSource { get => (IList)GetValue(ItemsSourceProperty); set => SetValue(ItemsSourceProperty, value); }
    public bool IsTimeMode { get => (bool)GetValue(IsTimeModeProperty); set => SetValue(IsTimeModeProperty, value); }

    public bool IsNormalMode => !IsTimeMode && ItemsSource == null;
    public bool IsPickerMode => !IsTimeMode && ItemsSource != null;

    private string _amPm = "am";
    public string AmPm
    {
        get => _amPm;
        set
        {
            _amPm = value;
            OnPropertyChanged();
            UpdateToggleUI(); // 状态改变时更新 UI
            UpdateText();
        }
    }

    // 逻辑：按下（选中）为 BtnBgMain，未按下（默认）为 BtnBgSec
    private void UpdateToggleUI()
    {
        if (AmBtn == null || PmBtn == null) return;

        if (AmPm == "am")
        {
            AmBtn.SetDynamicResource(Button.BackgroundColorProperty, "BtnBgMain");
            AmBtn.SetDynamicResource(Button.TextColorProperty, "TextMain");
            PmBtn.SetDynamicResource(Button.BackgroundColorProperty, "BtnBgSec");
            PmBtn.SetDynamicResource(Button.TextColorProperty, "TextSec");
        }
        else
        {
            PmBtn.SetDynamicResource(Button.BackgroundColorProperty, "BtnBgMain");
            PmBtn.SetDynamicResource(Button.TextColorProperty, "TextMain");
            AmBtn.SetDynamicResource(Button.BackgroundColorProperty, "BtnBgSec");
            AmBtn.SetDynamicResource(Button.TextColorProperty, "TextSec");
        }
    }

    private string _hourPart;
    public string HourPart { get => _hourPart; set { _hourPart = value; OnPropertyChanged(); UpdateText(); } }
    private string _minutePart;
    public string MinutePart { get => _minutePart; set { _minutePart = value; OnPropertyChanged(); UpdateText(); } }

    public InputBox()
    {
        InitializeComponent();
        UpdateToggleUI(); // 初始化 UI
    }

    private void SetAm(object sender, EventArgs e) => AmPm = "am";
    private void SetPm(object sender, EventArgs e) => AmPm = "pm";
    private void OnTimeChanged(object sender, TextChangedEventArgs e) => UpdateText();

    private void UpdateText()
    {
        if (int.TryParse(HourPart, out int h))
        {
            if (h > 12) h -= 12;
            if (h == 0) h = 12;
            string m = string.IsNullOrWhiteSpace(MinutePart) ? "00" : MinutePart.PadLeft(2, '0');
            Text = $"{h}.{m}{AmPm}";
        }
    }

    public new event PropertyChangedEventHandler PropertyChanged;
    protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        if (propertyName == nameof(IsTimeMode) || propertyName == nameof(ItemsSource))
        {
            OnPropertyChanged(nameof(IsNormalMode));
            OnPropertyChanged(nameof(IsPickerMode));
        }
    }
}