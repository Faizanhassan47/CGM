namespace CGM.PatientApp.Controls;

public partial class WeeklyReportsChart : ContentView
{
    public WeeklyReportsChart()
    {
        InitializeComponent();
    }

    private void OnDailyTapped(object? sender, TappedEventArgs e)
    {
        SetSelectedMode("Daily");
    }

    private void OnWeeklyTapped(object? sender, TappedEventArgs e)
    {
        SetSelectedMode("Weekly");
    }

    private void OnMonthlyTapped(object? sender, TappedEventArgs e)
    {
        SetSelectedMode("Monthly");
    }

    private void SetSelectedMode(string mode)
    {
        var primaryColor = Color.FromArgb("#01B4F1");
        var secondaryColor = Color.FromArgb("#583295");
        var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
        var activeBg = isDark ? Color.FromArgb("#583295") : Colors.White;
        var transparent = Colors.Transparent;

        DailyBorder.BackgroundColor = mode == "Daily" ? activeBg : transparent;
        DailyLabel.TextColor = mode == "Daily" ? primaryColor : secondaryColor;
        DailyLabel.FontAttributes = mode == "Daily" ? FontAttributes.Bold : FontAttributes.None;

        WeeklyBorder.BackgroundColor = mode == "Weekly" ? activeBg : transparent;
        WeeklyLabel.TextColor = mode == "Weekly" ? primaryColor : secondaryColor;
        WeeklyLabel.FontAttributes = mode == "Weekly" ? FontAttributes.Bold : FontAttributes.None;

        MonthlyBorder.BackgroundColor = mode == "Monthly" ? activeBg : transparent;
        MonthlyLabel.TextColor = mode == "Monthly" ? primaryColor : secondaryColor;
        MonthlyLabel.FontAttributes = mode == "Monthly" ? FontAttributes.Bold : FontAttributes.None;
    }
}

