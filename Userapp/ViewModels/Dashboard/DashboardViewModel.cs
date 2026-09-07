using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Reports;
using CGM.PatientApp.Services.Cgm;
using Microsoft.Maui.Devices;
using System.Windows.Input;

namespace CGM.PatientApp.ViewModels.Dashboard;

public record HistoryReadingItem(
    string TimeText,
    string ValueText,
    string StatusText,
    Color StatusBgColor,
    Color StatusTextColor,
    string TrendArrow,
    Color TrendColor
);

public partial class LogEventItem : ObservableObject
{
    [ObservableProperty]
    private string _eventType = "Meal";

    [ObservableProperty]
    private string _title = "";

    [ObservableProperty]
    private string _detail = "";

    [ObservableProperty]
    private string _timeText = "";

    [ObservableProperty]
    private string _iconText = "";

    [ObservableProperty]
    private Color _iconBgColor = Colors.Transparent;

    [ObservableProperty]
    private Color _iconColor = Colors.Black;
}

public partial class CalendarDayItem : ObservableObject
{
    [ObservableProperty]
    private string _dayName = "";

    [ObservableProperty]
    private string _dayNumber = "";

    [ObservableProperty]
    private DateTime _date;

    [ObservableProperty]
    private bool _isSelected;

    [ObservableProperty]
    private Color _dotColor = Color.FromArgb("#059669");

    [ObservableProperty]
    private string _tirPercent = "85%";
}

public partial class DashboardViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;
    private readonly IDeviceService _deviceService;
    private readonly ICgmDeviceService _cgmDeviceService;
    private readonly IGlucoseService _glucoseService;
    private readonly IAlertService _alertService;
    private readonly IPdfReportService? _pdfReportService;

    [ObservableProperty]
    private string _patientGreeting = "Good morning,";

    [ObservableProperty]
    private string _patientName = "Jane";

    [ObservableProperty]
    private string _userFullName = "Jane Doe";

    [ObservableProperty]
    private string _userEmail = "jane.doe@email.com";

    [ObservableProperty]
    private string _deviceStatusText = "Not connected";

    [ObservableProperty]
    private bool _isDeviceConnected = false;

    [ObservableProperty]
    private bool _isDeviceDisconnected = true;

    [ObservableProperty]
    private Color _statusBadgeBgColor = Color.FromArgb("#FEF3C7");

    [ObservableProperty]
    private Color _statusBadgeTextColor = Color.FromArgb("#D97706");

    [ObservableProperty]
    private string _statusBadgeIcon = "\uF127";

    [ObservableProperty]
    private string _batteryPercent = "--";

    [ObservableProperty]
    private string _currentGlucose = "--";

    [ObservableProperty]
    private Color _heroGradientStart = Color.FromArgb("#0D9488");

    [ObservableProperty]
    private Color _heroGradientEnd = Color.FromArgb("#10B981");

    [ObservableProperty]
    private string _velocityRateText = "In Target • Stable (±0.5 mg/dL/min)";

    partial void OnCurrentGlucoseChanged(string value)
    {
        UpdateHeroCardAura(value);
    }

    private void UpdateHeroCardAura(string value)
    {
        if (int.TryParse(value, out var g))
        {
            if (g > 250)
            {
                HeroGradientStart = Color.FromArgb("#991B1B"); // Urgent High crimson
                HeroGradientEnd = Color.FromArgb("#EF4444");
                VelocityRateText = "Urgent High • Check Ketones";
                TrendArrow = "↑↑";
            }
            else if (g > 180)
            {
                HeroGradientStart = Color.FromArgb("#D97706"); // Amber warning
                HeroGradientEnd = Color.FromArgb("#F59E0B");
                VelocityRateText = "Above Target • Rising";
                TrendArrow = "↗";
            }
            else if (g < 70)
            {
                HeroGradientStart = Color.FromArgb("#B91C1C"); // Hypo alert red
                HeroGradientEnd = Color.FromArgb("#DC2626");
                VelocityRateText = "Hypoglycemia • Take 15g Carbs";
                TrendArrow = "↓↓";
            }
            else
            {
                HeroGradientStart = Color.FromArgb("#0D9488"); // Optimal teal
                HeroGradientEnd = Color.FromArgb("#10B981");   // Emerald
                VelocityRateText = "In Target • Stable (±0.5 mg/dL/min)";
                TrendArrow = "→";
            }
        }
        else
        {
            HeroGradientStart = Color.FromArgb("#475569");
            HeroGradientEnd = Color.FromArgb("#64748B");
            VelocityRateText = "Connecting to sensor...";
            TrendArrow = "•";
        }
    }

    [ObservableProperty]
    private string _glucoseUnit = "mg/dL";

    [ObservableProperty]
    private string _trendStatus = "Stable";

    [ObservableProperty]
    private string _trendArrow = "→";

    [ObservableProperty]
    private string _lastUpdatedText = "Updated just now";

    [ObservableProperty]
    private string _activeTrendFilter = "3H";

    [ObservableProperty]
    private bool _isTrend3H = true;

    [ObservableProperty]
    private bool _isTrend6H = false;

    [ObservableProperty]
    private bool _isTrend12H = false;

    [ObservableProperty]
    private bool _isTrend24H = false;

    [ObservableProperty]
    private string _trendTime1 = "6:00 AM";

    [ObservableProperty]
    private string _trendTime2 = "7:00 AM";

    [ObservableProperty]
    private string _trendTime3 = "8:00 AM";

    [ObservableProperty]
    private string _trendTime4 = "9:00 AM";

    [ObservableProperty]
    private string _trendPathData = "M 10,75 C 30,70 50,75 80,60 C 110,45 130,68 160,65 C 190,62 210,72 240,68 C 260,65 270,68 275,65";

    [ObservableProperty]
    private Color _trendLineColor = Color.FromArgb("#0D9488");

    [ObservableProperty]
    private Thickness _trendDotMargin = new(0, 58, 40, 0);

    [ObservableProperty]
    private Color _trendDotColor = Color.FromArgb("#0D9488");

    [ObservableProperty]
    private string _timeInRange = "82%";

    [ObservableProperty]
    private string _sensorStatus = "Good";

    [ObservableProperty]
    private string _sensorDaysLeft = "7 days left";

    [ObservableProperty]
    private string _batteryDaysLeft = "About 2 days left";

    [ObservableProperty]
    private string _lastSyncTime = "9:41 AM";

    [ObservableProperty]
    private string _lastSyncDate = "Today";

    // 5 Navigation Tabs
    [ObservableProperty]
    private string _selectedTab = "Home";

    [ObservableProperty]
    private bool _isHomeSelected = true;

    [ObservableProperty]
    private bool _isHistorySelected = false;

    [ObservableProperty]
    private bool _isReportsSelected = false;

    [ObservableProperty]
    private bool _isAlertsSelected = false;

    [ObservableProperty]
    private bool _isProfileSelected = false;

    // History Filters & State
    [ObservableProperty]
    private string _activeHistoryFilter = "Today";

    [ObservableProperty]
    private bool _isHistoryToday = true;

    [ObservableProperty]
    private bool _isHistory7D = false;

    [ObservableProperty]
    private bool _isHistory14D = false;

    [ObservableProperty]
    private bool _isHistory30D = false;

    [ObservableProperty]
    private ObservableCollection<HistoryReadingItem> _historyReadings = new();

    // Reports Filters & State
    [ObservableProperty]
    private string _activeReportFilter = "7D";

    [ObservableProperty]
    private bool _isReport7D = true;

    [ObservableProperty]
    private bool _isReport14D = false;

    [ObservableProperty]
    private bool _isReport30D = false;

    [ObservableProperty]
    private bool _isReport90D = false;

    [ObservableProperty]
    private bool _isReportCustom = false;

    [ObservableProperty]
    private string _reportDateRangeText = "May 2 – May 8, 2025";

    [ObservableProperty]
    private string _reportAvgGlucose = "96 mg/dL";

    [ObservableProperty]
    private string _reportAvgDiffText = "↓ 6 vs previous 7 days";

    [ObservableProperty]
    private string _reportHighest = "152 mg/dL";

    [ObservableProperty]
    private string _reportHighestDate = "May 10, 9:15 AM";

    [ObservableProperty]
    private string _reportLowest = "64 mg/dL";

    [ObservableProperty]
    private string _reportLowestDate = "May 8, 4:12 AM";

    [ObservableProperty]
    private string _reportTimeInRange = "82%";

    [ObservableProperty]
    private string _reportEstimatedA1c = "5.6%";

    [ObservableProperty]
    private string _reportGlucoseVariability = "28%";

    // Alerts Filters & State
    [ObservableProperty]
    private string _activeAlertFilter = "All";

    [ObservableProperty]
    private bool _isAlertAll = true;

    [ObservableProperty]
    private bool _isAlertCritical = false;

    [ObservableProperty]
    private bool _isAlertUnread = false;

    [ObservableProperty]
    private bool _isAlertInfo = false;

    // Daily Summary Stats
    [ObservableProperty]
    private string _avgGlucose = "96";

    [ObservableProperty]
    private string _avgDiffText = "↓ 6 vs yesterday";

    [ObservableProperty]
    private string _highestGlucose = "152";

    [ObservableProperty]
    private string _highestTime = "9:15 AM";


    
    [ObservableProperty]
    private Color _currentGlucoseColor = Color.FromArgb("#1F2937"); // Dark gray default

    [ObservableProperty]
    private Color _glucoseAuraColor = Colors.Transparent;

    [ObservableProperty]
    private string _lowestGlucose = "64";

    [ObservableProperty]
    private string _lowestTime = "4:12 AM";

    // Quick Event Logging
    [ObservableProperty]
    private ObservableCollection<LogEventItem> _recentEvents = new();

    // Weekly Calendar Strip
    [ObservableProperty]
    private ObservableCollection<CalendarDayItem> _weeklyCalendarDays = new();

    [ObservableProperty]
    private CalendarDayItem? _selectedCalendarDay;

    [ObservableProperty]
    private string _selectedDateFormattedText = "Today";

    [ObservableProperty]
    private bool _canGoToPreviousDay = true;

    [ObservableProperty]
    private bool _canGoToNextDay = false;

    // Clinical Time In Range (AGP Profile)
    [ObservableProperty]
    private string _tirVeryLowPercent = "2%";

    [ObservableProperty]
    private string _tirLowPercent = "4%";

    [ObservableProperty]
    private string _tirInRangePercent = "84%";

    [ObservableProperty]
    private string _tirHighPercent = "8%";

    [ObservableProperty]
    private string _tirVeryHighPercent = "2%";

    [ObservableProperty]
    private string _sensorDaysRemainingText = "Day 4 of 14 • 10 days left";

    [ObservableProperty]
    private string _sensorSignalStatusText = "BLE Connected";

    private IDispatcherTimer? _liveSimulationTimer;

    [ObservableProperty]
    private bool _isPrivacySecurityOverlayVisible;

    [ObservableProperty]
    private string _currentPassword = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmNewPassword = string.Empty;

    [ObservableProperty]
    private bool _isCurrentPasswordHidden = true;

    [ObservableProperty]
    private bool _isNewPasswordHidden = true;

    [ObservableProperty]
    private bool _isConfirmNewPasswordHidden = true;

    [ObservableProperty]
    private string _currentPasswordIcon = FontAwesomeIcons.Eye;

    [ObservableProperty]
    private string _newPasswordIcon = FontAwesomeIcons.Eye;

    [ObservableProperty]
    private string _confirmNewPasswordIcon = FontAwesomeIcons.Eye;

    [ObservableProperty]
    private bool _isPasswordChangeBusy;

    [ObservableProperty]
    private string _securityErrorMessage = string.Empty;

    public bool HasSecurityError => !string.IsNullOrWhiteSpace(SecurityErrorMessage);

    partial void OnSecurityErrorMessageChanged(string value) => OnPropertyChanged(nameof(HasSecurityError));

    public DashboardViewModel(
        IAuthService authService,
        IProfileService profileService,
        IDeviceService deviceService,
        ICgmDeviceService cgmDeviceService,
        IGlucoseService glucoseService,
        IAlertService alertService,
        IPdfReportService? pdfReportService = null)
    {
        _authService = authService;
        _profileService = profileService;
        _deviceService = deviceService;
        _cgmDeviceService = cgmDeviceService;
        _glucoseService = glucoseService;
        _alertService = alertService;
        _pdfReportService = pdfReportService ?? new PdfReportService();
        Title = "GlucoTrack Dashboard";

        _cgmDeviceService.RawMeasurementReceived += OnRawMeasurementReceived;

        InitializeWeeklyCalendar();
        InitializeRecentEvents();
        PopulateHistoryReadings("Today");
        UpdateTrendChart("3H");
    }

    private void OnRawMeasurementReceived(object? sender, CgmRawMeasurement m)
    {
        Application.Current?.Dispatcher.Dispatch(async () =>
        {
            CurrentGlucose = m.GlucoseValueMgDl.ToString("0");
            LastUpdatedText = "Updated just now";
            UpdateTrendChart(ActiveTrendFilter ?? "3H", (int)m.GlucoseValueMgDl);

            if (m.GlucoseValueMgDl < 55)
            {
                TriggerHaptic();
                await Shell.Current.DisplayAlert("⚠️ URGENT LOW", $"Your glucose is critically low ({m.GlucoseValueMgDl} mg/dL). Treat immediately with fast-acting carbs.", "Acknowledge");
            }
            else if (m.GlucoseValueMgDl > 250)
            {
                TriggerHaptic();
                await Shell.Current.DisplayAlert("⚠️ HIGH GLUCOSE", $"Your glucose is very high ({m.GlucoseValueMgDl} mg/dL). Consider checking ketones or taking insulin.", "Acknowledge");
            }
        });
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var hour = DateTime.Now.Hour;
        PatientGreeting = hour switch
        {
            < 12 => "Good morning,",
            < 17 => "Good afternoon,",
            _ => "Good evening,"
        };

        await RefreshUserDataAsync();
        await RefreshDeviceStateAsync();
        await RefreshGlucoseSummaryAsync();

        if (string.IsNullOrEmpty(SelectedTab))
        {
            SelectTab("Home");
        }

        UpdateTrendChart(ActiveTrendFilter ?? "3H");
        if (string.Equals(Environment.GetEnvironmentVariable("CGM_USE_MOCK_SERVICES"), "true", StringComparison.OrdinalIgnoreCase))
            StartLiveSimulationTimer();
    }

    private async Task RefreshDeviceStateAsync()
    {
        var device = await _deviceService.GetConfiguredDeviceAsync();
        if (device is null)
        {
            DeviceStatusText = "Not connected";
            LastUpdatedText = "No sensor connected";
            CurrentGlucose = "--";
            TrendStatus = "Disconnected";
            TrendArrow = "•";
            BatteryPercent = "--";
            SensorStatus = "Not Paired";
            SensorDaysLeft = "No sensor active";
            IsDeviceConnected = false;
            IsDeviceDisconnected = true;
            StatusBadgeBgColor = Color.FromArgb("#FEF3C7"); // Amber
            StatusBadgeTextColor = Color.FromArgb("#D97706");
            StatusBadgeIcon = "\uF127"; // FaLinkSlash
            return;
        }

        IsDeviceConnected = true;
        IsDeviceDisconnected = false;
        StatusBadgeBgColor = Color.FromArgb("#D1FAE5"); // Light green
        StatusBadgeTextColor = Color.FromArgb("#065F46"); // Dark green
        StatusBadgeIcon = "\uF0C1"; // FaLink
        DeviceStatusText = device.IsDataStale ? "Data stale" : device.ConnectionState.ToString();
        BatteryPercent = $"{device.BatteryVoltageMv} mV";
        SensorStatus = "Active";
        SensorDaysLeft = "14 days left";
        LastUpdatedText = device.LastCommunicationTime.HasValue
            ? $"Updated {FormatAge(DateTime.UtcNow - device.LastCommunicationTime.Value)}"
            : "Connected";
    }

    private static string FormatAge(TimeSpan age) => age.TotalMinutes < 1
        ? "just now"
        : age.TotalHours < 1 ? $"{(int)age.TotalMinutes} min ago" : $"{(int)age.TotalHours} hr ago";

    private void StartLiveSimulationTimer()
    {
        if (_liveSimulationTimer != null) return;

        try
        {
            _liveSimulationTimer = Application.Current?.Dispatcher.CreateTimer();
            if (_liveSimulationTimer != null)
            {
                _liveSimulationTimer.Interval = TimeSpan.FromSeconds(5);
                _liveSimulationTimer.Tick += (s, e) =>
                {
                    if (int.TryParse(CurrentGlucose, out var currentVal))
                    {
                        var delta = Random.Shared.Next(-2, 3);
                        var newVal = Math.Clamp(currentVal + delta, 72, 168);
                        CurrentGlucose = newVal.ToString();
                        LastUpdatedText = "Updated just now";

                        UpdateTrendChart(ActiveTrendFilter ?? "3H", newVal);
                    }
                };
                _liveSimulationTimer.Start();
            }
        }
        catch { /* Handled for headless/test environments */ }
    }

    public void StopBackgroundWork()
    {
        _liveSimulationTimer?.Stop();
        _liveSimulationTimer = null;
    }

    private static void TriggerHaptic()
    {
        try
        {
            HapticFeedback.Default.Perform(HapticFeedbackType.Click);
        }
        catch
        {
            // Non-critical, gracefully handled for unsupported platforms / tests
        }
    }

    public async Task RefreshUserDataAsync()
    {
        try
        {
            var user = await _authService.GetCurrentUserAsync();
            var profile = await _profileService.GetProfileAsync();

            var fullName = !string.IsNullOrWhiteSpace(profile?.FullName)
                ? profile.FullName
                : user?.FullName;

            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = Preferences.Default.Get("cgm_user_name", "Jane Doe");
            }

            if (string.IsNullOrWhiteSpace(fullName))
            {
                fullName = "Jane Doe";
            }

            UserFullName = fullName;

            var email = !string.IsNullOrWhiteSpace(profile?.Email)
                ? profile.Email
                : user?.Email;

            if (string.IsNullOrWhiteSpace(email))
            {
                email = Preferences.Default.Get("cgm_user_email", "jane.doe@email.com");
            }

            if (string.IsNullOrWhiteSpace(email))
            {
                email = "jane.doe@email.com";
            }

            UserEmail = email;

            // Extract first name for greeting, handling initials like "M"
            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length > 1 && parts[0].Length <= 2)
            {
                PatientName = parts[1]; // e.g. "Faizan" from "M Faizan"
            }
            else
            {
                PatientName = parts.Length > 0 ? parts[0] : fullName;
            }

            if (profile != null)
            {
                GlucoseUnit = profile.PreferredGlucoseUnit == Enums.GlucoseUnit.MmolL ? "mmol/L" : "mg/dL";
                if (IsDeviceConnected)
                {
                    CurrentGlucose = GlucoseUnitConverter.Convert(112, Enums.GlucoseUnit.MgDl, profile.PreferredGlucoseUnit)
                        .ToString(profile.PreferredGlucoseUnit == Enums.GlucoseUnit.MmolL ? "0.0" : "0");
                }
            }
            else
            {
                GlucoseUnit = "mg/dL";
                if (IsDeviceConnected) CurrentGlucose = "112";
            }
        }
        catch
        {
            if (string.IsNullOrWhiteSpace(UserFullName)) UserFullName = "Jane Doe";
            if (string.IsNullOrWhiteSpace(UserEmail)) UserEmail = "jane.doe@email.com";
            if (string.IsNullOrWhiteSpace(PatientName)) PatientName = "Jane";
        }
    }

    public async Task RefreshGlucoseSummaryAsync()
    {
        try
        {
            var summary = await _glucoseService.GetDashboardSummaryAsync();
            if (summary.LatestReading != null)
            {
                CurrentGlucose = summary.LatestReading.GlucoseValue.ToString("0");
                LastUpdatedText = $"Updated {FormatAge(DateTime.UtcNow - summary.LatestReading.MeasurementTime)}";
            }
            else
            {
                CurrentGlucose = "--";
                LastUpdatedText = "No data available";
            }
            
            AvgGlucose = summary.AverageGlucose > 0 ? summary.AverageGlucose.ToString("0") : "--";
            HighestGlucose = summary.HighestGlucose > 0 ? summary.HighestGlucose.ToString("0") : "--";
            LowestGlucose = summary.LowestGlucose > 0 && summary.LowestGlucose < 1000 ? summary.LowestGlucose.ToString("0") : "--";
            TimeInRange = summary.AverageGlucose > 0 ? $"{summary.TimeInRangePercentage:0}%" : "--%";
        }
        catch
        {
            CurrentGlucose = "--";
            LastUpdatedText = "Error fetching data";
        }
    }

    [RelayCommand]
    private void SetTrendFilter(string filter)
    {
        TriggerHaptic();
        UpdateTrendChart(filter);
    }

    public void UpdateTrendChart(string period, int? overrideLatest = null)
    {
        ActiveTrendFilter = period;
        IsTrend3H = period == "3H";
        IsTrend6H = period == "6H";
        IsTrend12H = period == "12H";
        IsTrend24H = period == "24H";

        var now = DateTime.Now;

        List<double> values;
        switch (period)
        {
            case "6H":
                TrendTime1 = now.AddHours(-6).ToString("h:mm tt");
                TrendTime2 = now.AddHours(-4).ToString("h:mm tt");
                TrendTime3 = now.AddHours(-2).ToString("h:mm tt");
                TrendTime4 = now.ToString("h:mm tt");
                values = new List<double> { 88, 92, 84, 78, 92, 128, 162, 148, 126, 114, 118, 106, 99, 104, 110, 114 };
                break;

            case "12H":
                TrendTime1 = now.AddHours(-12).ToString("h:mm tt");
                TrendTime2 = now.AddHours(-8).ToString("h:mm tt");
                TrendTime3 = now.AddHours(-4).ToString("h:mm tt");
                TrendTime4 = now.ToString("h:mm tt");
                values = new List<double> { 108, 102, 94, 86, 82, 79, 86, 112, 154, 172, 158, 132, 118, 114, 108, 124, 140, 128, 116, 114 };
                break;

            case "24H":
                TrendTime1 = now.AddHours(-24).ToString("h:mm tt");
                TrendTime2 = now.AddHours(-16).ToString("h:mm tt");
                TrendTime3 = now.AddHours(-8).ToString("h:mm tt");
                TrendTime4 = now.ToString("h:mm tt");
                values = new List<double> { 112, 118, 104, 96, 88, 80, 78, 84, 96, 132, 160, 148, 126, 116, 104, 118, 146, 162, 138, 120, 110, 106, 114 };
                break;

            case "3H":
            default:
                TrendTime1 = now.AddHours(-3).ToString("h:mm tt");
                TrendTime2 = now.AddHours(-2).ToString("h:mm tt");
                TrendTime3 = now.AddHours(-1).ToString("h:mm tt");
                TrendTime4 = now.ToString("h:mm tt");
                values = new List<double> { 76, 82, 108, 156, 182, 158, 126, 110, 114, 106, 94, 100, 110, 114 };
                break;
        }

        if (overrideLatest.HasValue)
        {
            values[values.Count - 1] = overrideLatest.Value;
        }
        else if (int.TryParse(CurrentGlucose, out var parsed))
        {
            values[values.Count - 1] = parsed;
        }

        var minVal = 45.0;
        var maxVal = 220.0;
        var startX = 10.0;
        var endX = 275.0;
        var topY = 12.0;
        var bottomY = 118.0;

        var points = new List<(double X, double Y)>();
        for (int i = 0; i < values.Count; i++)
        {
            var x = startX + ((endX - startX) * i / (values.Count - 1));
            var clamped = Math.Clamp(values[i], minVal, maxVal);
            var y = bottomY - ((clamped - minVal) / (maxVal - minVal) * (bottomY - topY));
            points.Add((x, y));
        }

        TrendPathData = GenerateSplinePath(points);

        var lastVal = values.Last();
        var lastPoint = points.Last();
        TrendDotMargin = new Thickness(0, Math.Clamp(lastPoint.Y - 7, 2, 116), 40, 0);

        if (lastVal > 250)
        {
            TrendLineColor = Color.FromArgb("#DC2626"); // Red
            TrendDotColor = Color.FromArgb("#DC2626");
            TrendStatus = "High ↑";
            CurrentGlucoseColor = Color.FromArgb("#DC2626");
            GlucoseAuraColor = Color.FromArgb("#33DC2626"); // 20% opacity red
        }
        else if (lastVal > 180)
        {
            TrendLineColor = Color.FromArgb("#D97706"); // Amber
            TrendDotColor = Color.FromArgb("#D97706");
            TrendStatus = "High ↗";
            CurrentGlucoseColor = Color.FromArgb("#D97706");
            GlucoseAuraColor = Color.FromArgb("#33D97706"); // 20% opacity amber
        }
        else if (lastVal < 55)
        {
            TrendLineColor = Color.FromArgb("#DC2626"); // Red
            TrendDotColor = Color.FromArgb("#DC2626");
            TrendStatus = "Low ↓";
            CurrentGlucoseColor = Color.FromArgb("#DC2626");
            GlucoseAuraColor = Color.FromArgb("#33DC2626"); 
        }
        else if (lastVal < 70)
        {
            TrendLineColor = Color.FromArgb("#2563EB"); // Blue
            TrendDotColor = Color.FromArgb("#2563EB");
            TrendStatus = "Low ↘";
            CurrentGlucoseColor = Color.FromArgb("#2563EB");
            GlucoseAuraColor = Color.FromArgb("#332563EB");
        }
        else
        {
            TrendLineColor = Color.FromArgb("#059669"); // Green
            TrendDotColor = Color.FromArgb("#059669");
            TrendStatus = "Stable →";
            CurrentGlucoseColor = Color.FromArgb("#1F2937"); // Normal dark text
            GlucoseAuraColor = Color.FromArgb("#11059669"); // Very faint green aura
        }
    }

    private static string GenerateSplinePath(List<(double X, double Y)> points)
    {
        if (points == null || points.Count == 0)
            return "M 10,75 L 275,75";

        if (points.Count == 1)
            return $"M 10,{points[0].Y:F1} L 275,{points[0].Y:F1}";

        var sb = new System.Text.StringBuilder();
        sb.Append($"M {points[0].X:F1},{points[0].Y:F1}");

        for (int i = 0; i < points.Count - 1; i++)
        {
            var p0 = i > 0 ? points[i - 1] : points[i];
            var p1 = points[i];
            var p2 = points[i + 1];
            var p3 = i < points.Count - 2 ? points[i + 2] : p2;

            var cp1X = p1.X + (p2.X - p0.X) / 6.0;
            var cp1Y = p1.Y + (p2.Y - p0.Y) / 6.0;

            var cp2X = p2.X - (p3.X - p1.X) / 6.0;
            var cp2Y = p2.Y - (p3.Y - p1.Y) / 6.0;

            sb.Append($" C {cp1X:F1},{cp1Y:F1} {cp2X:F1},{cp2Y:F1} {p2.X:F1},{p2.Y:F1}");
        }

        return sb.ToString();
    }

    [RelayCommand]
    private void SetHistoryFilter(string filter)
    {
        TriggerHaptic();
        ActiveHistoryFilter = filter;
        IsHistoryToday = filter == "Today";
        IsHistory7D = filter == "7D";
        IsHistory14D = filter == "14D";
        IsHistory30D = filter == "30D";

        PopulateHistoryReadings(filter);
    }

    private void PopulateHistoryReadings(string filter)
    {
        HistoryReadings.Clear();

        var greenBg = Color.FromArgb("#E6F4EA");
        var greenText = Color.FromArgb("#059669");
        var blueBg = Color.FromArgb("#EFF6FF");
        var blueText = Color.FromArgb("#2563EB");
        var amberBg = Color.FromArgb("#FEF3C7");
        var amberText = Color.FromArgb("#D97706");
        var redBg = Color.FromArgb("#FEE2E2");
        var redText = Color.FromArgb("#DC2626");

        switch (filter)
        {
            case "7D":
                AvgGlucose = "98";
                AvgDiffText = "↓ 4 vs prev 7D";
                HighestGlucose = "164";
                HighestTime = "May 3, 2:10 PM";
                LowestGlucose = "62";
                LowestTime = "May 5, 3:30 AM";
                TimeInRange = "85%";

                HistoryReadings.Add(new("Wed, May 8", "92", "Optimal", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Tue, May 7", "96", "Optimal", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Mon, May 6", "93", "In Range", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Sun, May 5", "97", "Optimal", greenBg, greenText, "↗", greenText));
                HistoryReadings.Add(new("Sat, May 4", "95", "Optimal", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Fri, May 3", "98", "In Range", greenBg, greenText, "↗", greenText));
                HistoryReadings.Add(new("Thu, May 2", "102", "In Range", greenBg, greenText, "→", greenText));
                break;

            case "14D":
                AvgGlucose = "101";
                AvgDiffText = "↓ 2 vs prev 14D";
                HighestGlucose = "178";
                HighestTime = "Apr 28, 1:45 PM";
                LowestGlucose = "58";
                LowestTime = "May 1, 4:00 AM";
                TimeInRange = "80%";

                HistoryReadings.Add(new("Week 2 Avg", "96", "Optimal", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Week 1 Avg", "105", "In Range", greenBg, greenText, "↗", greenText));
                HistoryReadings.Add(new("Peak Day (Apr 28)", "132", "Elevated", amberBg, amberText, "↑", amberText));
                HistoryReadings.Add(new("Best Day (May 4)", "89", "Optimal", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Overnight Lows", "64", "Low", redBg, redText, "↘", redText));
                break;

            case "30D":
                AvgGlucose = "104";
                AvgDiffText = "↑ 1 vs prev month";
                HighestGlucose = "185";
                HighestTime = "Apr 15, 8:20 PM";
                LowestGlucose = "55";
                LowestTime = "Apr 20, 3:15 AM";
                TimeInRange = "79%";

                HistoryReadings.Add(new("Monthly Avg", "104", "In Range", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Week 4 Avg", "96", "Optimal", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Week 3 Avg", "102", "In Range", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("Week 2 Avg", "108", "In Range", greenBg, greenText, "↗", greenText));
                HistoryReadings.Add(new("Week 1 Avg", "110", "In Range", greenBg, greenText, "→", greenText));
                break;

            case "Today":
            default:
                AvgGlucose = "96";
                AvgDiffText = "↓ 6 vs yesterday";
                HighestGlucose = "152";
                HighestTime = "9:15 AM";
                LowestGlucose = "64";
                LowestTime = "4:12 AM";
                TimeInRange = "82%";

                HistoryReadings.Add(new("9:30 AM", "112", "Stable", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("8:30 AM", "105", "Stable", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("7:30 AM", "98", "Stable", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("6:30 AM", "86", "In Range", greenBg, greenText, "↗", greenText));
                HistoryReadings.Add(new("5:30 AM", "78", "In Range", greenBg, greenText, "→", greenText));
                HistoryReadings.Add(new("4:30 AM", "68", "Low", redBg, redText, "↘", redText));
                break;
        }
    }

    [RelayCommand]
    private async Task SetReportFilterAsync(string filter)
    {
        TriggerHaptic();
        ActiveReportFilter = filter;
        IsReport7D = filter == "7D";
        IsReport14D = filter == "14D";
        IsReport30D = filter == "30D";
        IsReport90D = filter == "90D";
        IsReportCustom = filter == "Custom";

        switch (filter)
        {
            case "14D":
                ReportDateRangeText = "Apr 25 – May 8, 2025";
                ReportAvgGlucose = "101 mg/dL";
                ReportAvgDiffText = "↓ 4 vs previous 14 days";
                ReportHighest = "178 mg/dL";
                ReportHighestDate = "Apr 28, 1:45 PM";
                ReportLowest = "58 mg/dL";
                ReportLowestDate = "May 1, 4:00 AM";
                ReportTimeInRange = "80%";
                ReportEstimatedA1c = "5.8%";
                ReportGlucoseVariability = "31%";
                break;

            case "30D":
                ReportDateRangeText = "Apr 9 – May 8, 2025";
                ReportAvgGlucose = "104 mg/dL";
                ReportAvgDiffText = "↑ 2 vs previous 30 days";
                ReportHighest = "185 mg/dL";
                ReportHighestDate = "Apr 15, 8:20 PM";
                ReportLowest = "55 mg/dL";
                ReportLowestDate = "Apr 20, 3:15 AM";
                ReportTimeInRange = "79%";
                ReportEstimatedA1c = "5.9%";
                ReportGlucoseVariability = "33%";
                break;

            case "90D":
                ReportDateRangeText = "Feb 8 – May 8, 2025";
                ReportAvgGlucose = "107 mg/dL";
                ReportAvgDiffText = "↓ 1 vs previous 90 days";
                ReportHighest = "192 mg/dL";
                ReportHighestDate = "Mar 12, 10:30 AM";
                ReportLowest = "52 mg/dL";
                ReportLowestDate = "Feb 22, 2:40 AM";
                ReportTimeInRange = "77%";
                ReportEstimatedA1c = "6.1%";
                ReportGlucoseVariability = "34%";
                break;

            case "Custom":
                var start = await Shell.Current.DisplayPromptAsync("Custom Report", "Enter Start Date (YYYY-MM-DD):", initialValue: DateTime.Today.AddDays(-7).ToString("yyyy-MM-dd"));
                if (string.IsNullOrWhiteSpace(start)) return;
                var end = await Shell.Current.DisplayPromptAsync("Custom Report", "Enter End Date (YYYY-MM-DD):", initialValue: DateTime.Today.ToString("yyyy-MM-dd"));
                if (string.IsNullOrWhiteSpace(end)) return;

                ReportDateRangeText = $"{start} – {end}";
                ReportAvgGlucose = "98 mg/dL";
                ReportAvgDiffText = "Custom range summary";
                ReportHighest = "165 mg/dL";
                ReportHighestDate = $"{start}, 11:00 AM";
                ReportLowest = "61 mg/dL";
                ReportLowestDate = $"{end}, 4:00 AM";
                ReportTimeInRange = "83%";
                ReportEstimatedA1c = "5.7%";
                ReportGlucoseVariability = "29%";
                break;

            case "7D":
            default:
                ReportDateRangeText = "May 2 – May 8, 2025";
                ReportAvgGlucose = "96 mg/dL";
                ReportAvgDiffText = "↓ 6 vs previous 7 days";
                ReportHighest = "152 mg/dL";
                ReportHighestDate = "May 10, 9:15 AM";
                ReportLowest = "64 mg/dL";
                ReportLowestDate = "May 8, 4:12 AM";
                ReportTimeInRange = "82%";
                ReportEstimatedA1c = "5.6%";
                ReportGlucoseVariability = "28%";
                break;
        }
    }

    [RelayCommand]
    private void SetAlertFilter(string filter)
    {
        TriggerHaptic();
        ActiveAlertFilter = filter;
        IsAlertAll = filter == "All";
        IsAlertCritical = filter == "Critical";
        IsAlertUnread = filter == "Unread";
        IsAlertInfo = filter == "Info" || filter == "ℹ️ Info";
    }

    [RelayCommand]
    private Task OpenDetailAsync(string section) =>
        Shell.Current.GoToAsync($"AppDetailPage?section={Uri.EscapeDataString(section)}");

    [RelayCommand]
    private void OpenGlucoseDetails()
    {
        SelectTab("History");
    }


    [RelayCommand]
    private Task OpenAlertAsync(string alert) =>
        Shell.Current.GoToAsync($"AppDetailPage?section=Alert&item={Uri.EscapeDataString(alert)}");

    [RelayCommand]
    private Task OpenReadingAsync(string reading) =>
        Shell.Current.GoToAsync($"AppDetailPage?section=Reading&item={Uri.EscapeDataString(reading)}");

    [RelayCommand]
    public async Task ExportReportAsync()
    {
        try
        {
            IsBusy = true;

            var pdfService = _pdfReportService ?? new PdfReportService();
            var filePath = await pdfService.GenerateReportPdfAsync(
                patientName: UserFullName,
                patientEmail: UserEmail,
                dateRange: ReportDateRangeText,
                avgGlucose: ReportAvgGlucose,
                timeInRange: ReportTimeInRange,
                highestGlucose: ReportHighest,
                lowestGlucose: ReportLowest
            );

            IsBusy = false;

            // Trigger Native Share / Open Dialog
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "GlucoTrack Clinical AGP Report",
                File = new ShareFile(filePath)
            });
        }
        catch (Exception ex)
        {
            IsBusy = false;
            System.Diagnostics.Debug.WriteLine($"Report export failed: {ex}");
            await Shell.Current.DisplayAlertAsync("Export failed", "The report could not be generated or shared. Please try again.", "OK");
        }
    }

    [RelayCommand]
    private async Task ShareReportAsync()
    {
        await ExportReportAsync();
    }

    [RelayCommand]
    private void SelectTab(string tabName)
    {
        TriggerHaptic();
        SelectedTab = tabName;
        IsHomeSelected = tabName == "Home";
        IsHistorySelected = tabName == "History";
        IsReportsSelected = tabName == "Reports";
        IsAlertsSelected = tabName == "Alerts";
        IsProfileSelected = tabName == "Profile";

        // Dynamically refresh user and profile data
        _ = RefreshUserDataAsync();
    }

    [RelayCommand]
    private async Task NavigateToPairingAsync()
    {
        TriggerHaptic();
        await Shell.Current.GoToAsync("//DeviceSelectionPage");
    }

    [RelayCommand]
    private async Task LogoutAsync()
    {
        TriggerHaptic();
        await _authService.LogoutAsync();
        await Shell.Current.GoToAsync("//LoginPage");
    }

    [RelayCommand]
    private void OpenPrivacySecurity()
    {
        SecurityErrorMessage = string.Empty;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmNewPassword = string.Empty;
        ResetPasswordVisibility();
        IsPrivacySecurityOverlayVisible = true;
    }

    [RelayCommand]
    private void ToggleCurrentPasswordVisibility()
    {
        IsCurrentPasswordHidden = !IsCurrentPasswordHidden;
        CurrentPasswordIcon = IsCurrentPasswordHidden ? FontAwesomeIcons.Eye : FontAwesomeIcons.EyeSlash;
    }

    [RelayCommand]
    private void ToggleNewPasswordVisibility()
    {
        IsNewPasswordHidden = !IsNewPasswordHidden;
        NewPasswordIcon = IsNewPasswordHidden ? FontAwesomeIcons.Eye : FontAwesomeIcons.EyeSlash;
    }

    [RelayCommand]
    private void ToggleConfirmNewPasswordVisibility()
    {
        IsConfirmNewPasswordHidden = !IsConfirmNewPasswordHidden;
        ConfirmNewPasswordIcon = IsConfirmNewPasswordHidden ? FontAwesomeIcons.Eye : FontAwesomeIcons.EyeSlash;
    }

    private void ResetPasswordVisibility()
    {
        IsCurrentPasswordHidden = true;
        IsNewPasswordHidden = true;
        IsConfirmNewPasswordHidden = true;
        CurrentPasswordIcon = FontAwesomeIcons.Eye;
        NewPasswordIcon = FontAwesomeIcons.Eye;
        ConfirmNewPasswordIcon = FontAwesomeIcons.Eye;
    }

    [RelayCommand]
    private void ClosePrivacySecurity()
    {
        if (IsPasswordChangeBusy) return;
        IsPrivacySecurityOverlayVisible = false;
        CurrentPassword = string.Empty;
        NewPassword = string.Empty;
        ConfirmNewPassword = string.Empty;
        ResetPasswordVisibility();
        SecurityErrorMessage = string.Empty;
    }

    [RelayCommand]
    private async Task ChangePasswordAsync()
    {
        if (IsPasswordChangeBusy) return;
        SecurityErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(CurrentPassword))
        {
            SecurityErrorMessage = "Enter your current password.";
            return;
        }
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 6)
        {
            SecurityErrorMessage = "New password must be at least 6 characters.";
            return;
        }
        if (NewPassword != ConfirmNewPassword)
        {
            SecurityErrorMessage = "New passwords do not match.";
            return;
        }
        if (CurrentPassword == NewPassword)
        {
            SecurityErrorMessage = "New password must be different from the current password.";
            return;
        }

        try
        {
            IsPasswordChangeBusy = true;
            var result = await _authService.ChangePasswordAsync(CurrentPassword, NewPassword);
            if (!result.Success)
            {
                SecurityErrorMessage = result.Message;
                return;
            }

            IsPrivacySecurityOverlayVisible = false;
            await Shell.Current.DisplayAlertAsync("Password changed", result.Message, "Sign in");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        finally
        {
            IsPasswordChangeBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAccountAsync()
    {
        if (IsPasswordChangeBusy) return;
        var confirmed = await Shell.Current.DisplayAlertAsync(
            "Permanently delete account?",
            "This deletes your account, profile, devices, sensor records, glucose readings, and alerts. This action cannot be undone.",
            "Delete permanently",
            "Cancel");
        if (!confirmed) return;

        try
        {
            IsPasswordChangeBusy = true;
            SecurityErrorMessage = string.Empty;
            var result = await _authService.DeleteAccountAsync();
            if (!result.Success)
            {
                SecurityErrorMessage = result.Message;
                return;
            }

            IsPrivacySecurityOverlayVisible = false;
            await Shell.Current.GoToAsync("//LoginPage");
        }
        finally
        {
            IsPasswordChangeBusy = false;
        }
    }

    // ================= 1. QUICK EVENT LOGGING =================
    private void InitializeRecentEvents()
    {
        RecentEvents.Clear();
        RecentEvents.Add(new LogEventItem
        {
            EventType = "Insulin",
            Title = "Rapid Bolus Insulin",
            Detail = "3.5 units",
            TimeText = "8:35 AM",
            IconText = "\uF48E", // syringe
            IconBgColor = Color.FromArgb("#EFF6FF"),
            IconColor = Color.FromArgb("#2563EB")
        });
        RecentEvents.Add(new LogEventItem
        {
            EventType = "Meal",
            Title = "Breakfast Oatmeal & Berries",
            Detail = "42g carbs",
            TimeText = "8:15 AM",
            IconText = "\uF2E7", // utensils
            IconBgColor = Color.FromArgb("#FEF3C7"),
            IconColor = Color.FromArgb("#D97706")
        });
    }

    [RelayCommand]
    private async Task LogMealAsync()
    {
        TriggerHaptic();
        var carbsStr = await Shell.Current.DisplayPromptAsync(
            "Log Meal",
            "Enter carbohydrate amount (grams):",
            placeholder: "e.g. 45",
            keyboard: Keyboard.Numeric,
            initialValue: "45");

        if (string.IsNullOrWhiteSpace(carbsStr)) return;

        var mealName = await Shell.Current.DisplayPromptAsync(
            "Meal Description",
            "Enter meal description:",
            placeholder: "e.g. Lunch, Snack",
            initialValue: "Meal / Snack");

        if (string.IsNullOrWhiteSpace(mealName)) mealName = "Meal";

        RecentEvents.Insert(0, new LogEventItem
        {
            EventType = "Meal",
            Title = mealName,
            Detail = $"{carbsStr}g carbs",
            TimeText = DateTime.Now.ToString("h:mm tt"),
            IconText = "\uF2E7",
            IconBgColor = Color.FromArgb("#FEF3C7"),
            IconColor = Color.FromArgb("#D97706")
        });

        TriggerHaptic();
        await Shell.Current.DisplayAlertAsync("Meal Logged", $"Successfully recorded {carbsStr}g carbs for {mealName}.", "OK");
    }

    [RelayCommand]
    private async Task LogInsulinAsync()
    {
        TriggerHaptic();
        var doseStr = await Shell.Current.DisplayPromptAsync(
            "Log Insulin Dose",
            "Enter units of insulin (u):",
            placeholder: "e.g. 4.0",
            keyboard: Keyboard.Numeric,
            initialValue: "4.0");

        if (string.IsNullOrWhiteSpace(doseStr)) return;

        var insulinType = await Shell.Current.DisplayActionSheetAsync(
            "Select Insulin Type",
            "Cancel",
            null,
            "Rapid-Acting (Bolus)",
            "Long-Acting (Basal)",
            "Pre-Mixed");

        if (string.IsNullOrWhiteSpace(insulinType) || insulinType == "Cancel")
            insulinType = "Rapid-Acting (Bolus)";

        RecentEvents.Insert(0, new LogEventItem
        {
            EventType = "Insulin",
            Title = insulinType,
            Detail = $"{doseStr} units",
            TimeText = DateTime.Now.ToString("h:mm tt"),
            IconText = "\uF48E",
            IconBgColor = Color.FromArgb("#EFF6FF"),
            IconColor = Color.FromArgb("#2563EB")
        });

        TriggerHaptic();
        await Shell.Current.DisplayAlertAsync("Insulin Logged", $"Successfully recorded {doseStr} u ({insulinType}).", "OK");
    }

    [RelayCommand]
    private async Task LogActivityAsync()
    {
        TriggerHaptic();
        var minsStr = await Shell.Current.DisplayPromptAsync(
            "Log Exercise",
            "Enter duration in minutes:",
            placeholder: "e.g. 30",
            keyboard: Keyboard.Numeric,
            initialValue: "30");

        if (string.IsNullOrWhiteSpace(minsStr)) return;

        var activityType = await Shell.Current.DisplayActionSheetAsync(
            "Select Activity",
            "Cancel",
            null,
            "Brisk Walk",
            "Running / Jogging",
            "Cycling",
            "Strength Training",
            "Yoga / Stretching");

        if (string.IsNullOrWhiteSpace(activityType) || activityType == "Cancel")
            activityType = "Exercise";

        RecentEvents.Insert(0, new LogEventItem
        {
            EventType = "Exercise",
            Title = activityType,
            Detail = $"{minsStr} mins",
            TimeText = DateTime.Now.ToString("h:mm tt"),
            IconText = "\uF70C",
            IconBgColor = Color.FromArgb("#D1FAE5"),
            IconColor = Color.FromArgb("#059669")
        });

        TriggerHaptic();
        await Shell.Current.DisplayAlertAsync("Exercise Logged", $"Successfully recorded {minsStr} mins of {activityType}.", "OK");
    }

    // ================= 2. WEEKLY CALENDAR STRIP =================
    private void InitializeWeeklyCalendar()
    {
        WeeklyCalendarDays.Clear();
        var today = DateTime.Today;

        // 7 days: 6 days ago up to today
        for (int i = 6; i >= 0; i--)
        {
            var date = today.AddDays(-i);
            var isToday = i == 0;
            var dotColor = i switch
            {
                1 => Color.FromArgb("#D97706"), // amber
                4 => Color.FromArgb("#059669"), // green
                _ => Color.FromArgb("#059669")
            };
            var tir = i switch
            {
                1 => "68%",
                3 => "91%",
                5 => "88%",
                _ => "82%"
            };

            var item = new CalendarDayItem
            {
                DayName = date.ToString("ddd").ToUpper(),
                DayNumber = date.Day.ToString(),
                Date = date,
                IsSelected = isToday,
                DotColor = dotColor,
                TirPercent = tir
            };
            WeeklyCalendarDays.Add(item);
            if (isToday)
            {
                SelectedCalendarDay = item;
                SelectedDateFormattedText = $"{date:dddd, MMM d} (Today)";
                CanGoToNextDay = false;
                CanGoToPreviousDay = WeeklyCalendarDays.Count > 1;
            }
        }
    }

    [RelayCommand]
    private void PreviousDay()
    {
        if (SelectedCalendarDay == null || WeeklyCalendarDays.Count == 0) return;
        int idx = WeeklyCalendarDays.IndexOf(SelectedCalendarDay);
        if (idx > 0)
        {
            SelectCalendarDay(WeeklyCalendarDays[idx - 1]);
        }
    }

    [RelayCommand]
    private void NextDay()
    {
        if (SelectedCalendarDay == null || WeeklyCalendarDays.Count == 0) return;
        int idx = WeeklyCalendarDays.IndexOf(SelectedCalendarDay);
        if (idx < WeeklyCalendarDays.Count - 1)
        {
            SelectCalendarDay(WeeklyCalendarDays[idx + 1]);
        }
    }

    [RelayCommand]
    private void SelectCalendarDay(CalendarDayItem day)
    {
        if (day == null) return;
        TriggerHaptic();

        foreach (var d in WeeklyCalendarDays)
        {
            d.IsSelected = (d == day);
        }
        SelectedCalendarDay = day;

        int idx = WeeklyCalendarDays.IndexOf(day);
        CanGoToPreviousDay = idx > 0;
        CanGoToNextDay = idx < WeeklyCalendarDays.Count - 1;

        SelectedDateFormattedText = day.Date.Date == DateTime.Today
            ? $"{day.Date:dddd, MMM d} (Today)"
            : day.Date.Date == DateTime.Today.AddDays(-1)
                ? $"{day.Date:dddd, MMM d} (Yesterday)"
                : $"{day.Date:dddd, MMM d}";

        PopulateDayReadings(day);
    }

    private void PopulateDayReadings(CalendarDayItem day)
    {
        HistoryReadings.Clear();

        var greenBg = Color.FromArgb("#E6F4EA");
        var greenText = Color.FromArgb("#059669");
        var amberBg = Color.FromArgb("#FEF3C7");
        var amberText = Color.FromArgb("#D97706");
        var redBg = Color.FromArgb("#FEE2E2");
        var redText = Color.FromArgb("#DC2626");

        var isToday = day.Date.Date == DateTime.Today;
        var dayDateStr = day.Date.ToString("ddd, MMM d");

        if (isToday)
        {
            PopulateHistoryReadings("Today");
            return;
        }

        AvgGlucose = day.TirPercent == "68%" ? "124" : "97";
        AvgDiffText = $"{dayDateStr} summary";
        HighestGlucose = day.TirPercent == "68%" ? "188" : "154";
        HighestTime = $"{day.Date:MMM d}, 1:40 PM";
        LowestGlucose = day.TirPercent == "68%" ? "62" : "71";
        LowestTime = $"{day.Date:MMM d}, 4:15 AM";
        TimeInRange = day.TirPercent;

        HistoryReadings.Add(new($"{dayDateStr} 9:00 PM", "108", "Stable", greenBg, greenText, "→", greenText));
        HistoryReadings.Add(new($"{dayDateStr} 6:30 PM", "136", "In Range", greenBg, greenText, "↗", greenText));
        HistoryReadings.Add(new($"{dayDateStr} 1:40 PM", HighestGlucose, day.TirPercent == "68%" ? "High" : "In Range", day.TirPercent == "68%" ? amberBg : greenBg, day.TirPercent == "68%" ? amberText : greenText, "↑", day.TirPercent == "68%" ? amberText : greenText));
        HistoryReadings.Add(new($"{dayDateStr} 12:15 PM", "118", "Stable", greenBg, greenText, "→", greenText));
        HistoryReadings.Add(new($"{dayDateStr} 8:00 AM", "95", "Optimal", greenBg, greenText, "→", greenText));
        HistoryReadings.Add(new($"{dayDateStr} 4:15 AM", LowestGlucose, LowestGlucose == "62" ? "Low" : "Optimal", LowestGlucose == "62" ? redBg : greenBg, LowestGlucose == "62" ? redText : greenText, LowestGlucose == "62" ? "↘" : "→", LowestGlucose == "62" ? redText : greenText));
    }

    // ================= CLINICAL TARGET RANGE & SAFETY =================
    [ObservableProperty]
    private int _targetRangeLow = 70;

    [ObservableProperty]
    private int _targetRangeHigh = 180;

    [ObservableProperty]
    private string _targetRangeSummary = "70 — 180 mg/dL (ADA Standard)";

    // ================= LIVE SENSOR HARDWARE DIAGNOSTICS =================
    [ObservableProperty]
    private string _diagBatteryMv = "3,000 mV (96%)";

    [ObservableProperty]
    private string _diagTemperature = "32.5 °C (Normal)";

    [ObservableProperty]
    private string _diagSerial = "26082400C221";

    [ObservableProperty]
    private string _diagFirmware = "A100";

    [ObservableProperty]
    private string _diagWearDays = "Day 4 of 14 • 10 days left";

    [ObservableProperty]
    private double _diagWearProgress = 0.71;

    [ObservableProperty]
    private string _diagSignal = "-58 dBm (Strong BLE)";

    [RelayCommand]
    private void DecreaseTargetLow()
    {
        if (TargetRangeLow > 60)
        {
            TargetRangeLow -= 5;
            UpdateTargetSummary();
            TriggerHaptic();
        }
    }

    [RelayCommand]
    private void IncreaseTargetLow()
    {
        if (TargetRangeLow < 90 && TargetRangeLow + 5 < TargetRangeHigh)
        {
            TargetRangeLow += 5;
            UpdateTargetSummary();
            TriggerHaptic();
        }
    }

    [RelayCommand]
    private void DecreaseTargetHigh()
    {
        if (TargetRangeHigh > 140 && TargetRangeHigh - 5 > TargetRangeLow)
        {
            TargetRangeHigh -= 5;
            UpdateTargetSummary();
            TriggerHaptic();
        }
    }

    [RelayCommand]
    private void IncreaseTargetHigh()
    {
        if (TargetRangeHigh < 250)
        {
            TargetRangeHigh += 5;
            UpdateTargetSummary();
            TriggerHaptic();
        }
    }

    private void UpdateTargetSummary()
    {
        TargetRangeSummary = $"{TargetRangeLow} — {TargetRangeHigh} mg/dL (Custom Target)";
    }

    [RelayCommand]
    private async Task ShareDoctorReportAsync()
    {
        TriggerHaptic();
        try
        {
            if (_pdfReportService != null)
            {
                var pdfPath = await _pdfReportService.GenerateReportPdfAsync(
                    UserFullName,
                    UserEmail,
                    ReportDateRangeText,
                    ReportAvgGlucose,
                    ReportTimeInRange,
                    ReportHighest,
                    ReportLowest);

                if (File.Exists(pdfPath))
                {
                    await Share.Default.RequestAsync(new ShareFileRequest
                    {
                        Title = "Share AGP Report with Physician",
                        File = new ShareFile(pdfPath)
                    });
                    return;
                }
            }

            await Shell.Current.DisplayAlertAsync("AGP Doctor Report", "Clinical AGP 1-Page Summary generated successfully.", "OK");
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlertAsync("Share Report", $"Could not open share dialog: {ex.Message}", "OK");
        }
    }
}

public class AlertViewModel
{
    public string AlertTitle { get; set; } = string.Empty;
    public string AlertValue { get; set; } = string.Empty;
    public string AlertDescription { get; set; } = string.Empty;
    public string AlertTime { get; set; } = string.Empty;
    public string BadgeText { get; set; } = string.Empty;
    public string SubBadgeText { get; set; } = string.Empty;
    public string IconText { get; set; } = string.Empty;
    public Color IconBgColor { get; set; } = Colors.Transparent;
    public Color IconTextColor { get; set; } = Colors.Transparent;
    public Color BadgeBgColor { get; set; } = Colors.Transparent;
    public Color BadgeTextColor { get; set; } = Colors.Transparent;
    public Color DotColor { get; set; } = Colors.Transparent;
    public bool HasValue => !string.IsNullOrEmpty(AlertValue);
    public bool HasDescription => !string.IsNullOrEmpty(AlertDescription);
    public bool HasSubBadge => !string.IsNullOrEmpty(SubBadgeText);
    public ICommand? Command { get; set; }
    public object? CommandParameter { get; set; }
}
