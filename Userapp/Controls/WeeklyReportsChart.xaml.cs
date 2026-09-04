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
        var primaryColor = Color.FromArgb("#0D9488");
        var secondaryColor = Color.FromArgb("#64748B");
        var whiteColor = Colors.White;
        var transparent = Colors.Transparent;

        DailyBorder.BackgroundColor = mode == "Daily" ? whiteColor : transparent;
        DailyLabel.TextColor = mode == "Daily" ? primaryColor : secondaryColor;
        DailyLabel.FontAttributes = mode == "Daily" ? FontAttributes.Bold : FontAttributes.None;

        WeeklyBorder.BackgroundColor = mode == "Weekly" ? whiteColor : transparent;
        WeeklyLabel.TextColor = mode == "Weekly" ? primaryColor : secondaryColor;
        WeeklyLabel.FontAttributes = mode == "Weekly" ? FontAttributes.Bold : FontAttributes.None;

        MonthlyBorder.BackgroundColor = mode == "Monthly" ? whiteColor : transparent;
        MonthlyLabel.TextColor = mode == "Monthly" ? primaryColor : secondaryColor;
        MonthlyLabel.FontAttributes = mode == "Monthly" ? FontAttributes.Bold : FontAttributes.None;
    }
}

