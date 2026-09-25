using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Reports;
using CGM.PatientApp.Services.Cgm;
using CGM.PatientApp.Services.Diagnostics;
using Microsoft.Maui.Devices;
using System.Windows.Input;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using LiveChartsCore.Defaults;

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

public sealed record TrendBarItem(
    double Height,
    Color Color,
    string ValueText,
    string TimeText
);

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
    private Color _dotColor = Color.FromArgb("#583295");

    [ObservableProperty]
    private string _tirPercent = "85%";
}

public partial class DashboardViewModel : BaseViewModel, IDisposable
{
    private readonly IAuthService _authService;
    private readonly IProfileService _profileService;
    private readonly IDeviceService _deviceService;
    private readonly ICgmDeviceService _cgmDeviceService;
    private readonly IGlucoseService _glucoseService;
    private readonly RealtimeGlucoseService _realtimeGlucoseService;
    private readonly IAlertService _alertService;
    private readonly ICriticalAlertEngine? _criticalAlertEngine;
    private readonly CGM.PatientApp.Services.Sync.ISyncService? _syncService;
    private readonly ILocalMeasurementRepository? _localRepo;
    private readonly ISmartDietService? _smartDietService;
    private double? _latestGlucoseMgDl;
    private readonly IPdfReportService? _pdfReportService;
    private readonly IReportService? _reportService;
    private bool _isActive;
    private const int HistoryPageSize = 50;
    private int _historyPage;
    private int _historyTotalPages;
    private bool _isLoadingMoreHistory;
    private IDispatcherTimer? _ruleOf15Timer;

    [ObservableProperty]
    private string _patientGreeting = "Good morning,";

    [ObservableProperty]
    private string _patientName = "Patient";

    [ObservableProperty]
    private string _userFullName = "Account";

    [ObservableProperty]
    private string _userEmail = string.Empty;

    [ObservableProperty]
    private string _deviceStatusText = "Not connected";

    [ObservableProperty]
    private bool _isDeviceConnected = false;

    [ObservableProperty]
    private bool _isDeviceDisconnected = true;

    [ObservableProperty]
    private Color _statusBadgeBgColor = Color.FromArgb("#583295");

    [ObservableProperty]
    private Color _statusBadgeTextColor = Color.FromArgb("#FFFFFF");

    [ObservableProperty]
    private string _statusBadgeIcon = "\uF127";

    [ObservableProperty]
    private string _batteryPercent = "--";

    [ObservableProperty]
    private string _currentGlucose = "--";

    [ObservableProperty]
    private Color _heroCardColor = Color.FromArgb("#01B4F1");

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
                HeroCardColor = Color.FromArgb("#583295");
                VelocityRateText = "Urgent High • Check Ketones";
                TrendArrow = "↑↑";
                MealRecommendationColor = Color.FromArgb("#F43F5E");
            }
            else if (g > 130)
            {
                HeroCardColor = Color.FromArgb("#583295");
                VelocityRateText = "Above Target • Rising";
                TrendArrow = "↗";
                MealRecommendationColor = Color.FromArgb("#583295");
            }
            else if (g < 90)
            {
                HeroCardColor = Color.FromArgb("#583295");
                VelocityRateText = "Hypoglycemia • Take 15g Carbs";
                TrendArrow = "↓↓";
                MealRecommendationColor = Color.FromArgb("#F43F5E");
            }
            else
            {
                HeroCardColor = Color.FromArgb("#01B4F1");
                VelocityRateText = "In Target • Stable (±0.5 mg/dL/min)";
                TrendArrow = "→";
                MealRecommendationColor = Color.FromArgb("#159B69");
            }

            if (_smartDietService != null)
            {
                // Fetch dynamic recommendation using Spoonacular API
                SafeAsync.Run(async () =>
                {
                    var rec = await _smartDietService.GetRecommendationAsync(g);
                    MainThread.BeginInvokeOnMainThread(() =>
                    {
                        MealRecommendationTitle = rec.Title;
                        MealRecommendationText = rec.Description;
                        MealRecommendationIcon = g < 90 ? "\uf0fc" : (g > 130 ? "\uf70c" : "\uf5d1");
                    });
                }, "Spoonacular.GetRecommendation");
            }
        }
        else
        {
            HeroCardColor = Color.FromArgb("#583295");
            VelocityRateText = "Connecting to sensor...";
            TrendArrow = "•";
        }
    }

    [ObservableProperty]
    private string _mealRecommendationTitle = "Connecting...";

    [ObservableProperty]
    private string _mealRecommendationText = "Waiting for glucose data to provide meal recommendations.";

    [ObservableProperty]
    private string _mealRecommendationIcon = "\uf2e7"; // Utensils

    [ObservableProperty]
    private Color _mealRecommendationColor = Color.FromArgb("#918699");

    [ObservableProperty]
    private string _glucoseUnit = "mg/dL";

    [ObservableProperty]
    private string _trendStatus = "No reading";

    [ObservableProperty]
    private string _trendArrow = "→";

    [ObservableProperty]
    private string _lastUpdatedText = "No recent reading";

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
    private ISeries[] _trendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _trendXAxes = new Axis[]
    {
        new Axis
        {
            Labeler = value => new DateTime((long)value).ToString("h:mm tt"),
            TextSize = 10,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#583295")),
            MinStep = TimeSpan.FromHours(1).Ticks
        }
    };

    [ObservableProperty]
    private Axis[] _trendYAxes = new Axis[]
    {
        new Axis
        {
            MinLimit = 40,
            MaxLimit = 300,
            MinStep = 40,
            TextSize = 10,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#583295")),
            CustomSeparators = new double[] { 90, 130 },
            SeparatorsPaint = new SolidColorPaint(SKColor.Parse("#FFFFFF")) { StrokeThickness = 1, PathEffect = new LiveChartsCore.SkiaSharpView.Painting.Effects.DashEffect(new float[] { 3, 3 }) }
        }
    };

    [ObservableProperty]
    private RectangularSection[] _trendSections = new RectangularSection[]
    {
        new RectangularSection
        {
            Yi = 90,
            Yj = 130,
            Fill = new SolidColorPaint(SKColor.Parse("#01B4F1").WithAlpha(20))
        }
    };

    [ObservableProperty]
    private string _trendWindowAverage = "116 mg/dL";

    [ObservableProperty]
    private string _trendWindowMin = "78";

    [ObservableProperty]
    private string _trendWindowMax = "164";

    [ObservableProperty]
    private string _trendWindowTir = "88%";

    // Emergency Hypoglycemia "Rule of 15" Protocol
    [ObservableProperty]
    private bool _isRuleOf15Active;

    [ObservableProperty]
    private int _ruleOf15SecondsRemaining = 900;

    [ObservableProperty]
    private string _ruleOf15TimerText = "15:00";

    [ObservableProperty]
    private double _ruleOf15Progress = 1.0;

    [ObservableProperty]
    private string _ruleOf15Status = "Ready: Consume 15g fast-acting carbs";

    [ObservableProperty]
    private string _emergencyCaregiverPhone = "1-800-555-0199";

    // Sensor Lifespan & Wear Gauge
    [ObservableProperty]
    private int _sensorDaysActive = 8;

    [ObservableProperty]
    private int _sensorTotalDays = 14;

    [ObservableProperty]
    private double _sensorLifespanProgress = 0.57;

    [ObservableProperty]
    private string _sensorLifespanPercentText = "Day 8 of 14 • 6 days left";

    [ObservableProperty]
    private Color _sensorLifespanBarColor = Color.FromArgb("#01B4F1");

    [ObservableProperty]
    private string _sensorWarmUpStatusText = "Active & Calibrated";

    [ObservableProperty]
    private string _timeInRange = "--";

    [ObservableProperty]
    private string _sensorStatus = "Not connected";

    [ObservableProperty]
    private string _sensorDaysLeft = "No active sensor";

    [ObservableProperty]
    private string _batteryDaysLeft = "Unavailable";

    [ObservableProperty]
    private string _lastSyncTime = "--";

    [ObservableProperty]
    private string _lastSyncDate = "Not synchronized";

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

    // History Excursion Filter (All, Lows, Highs, Target)
    [ObservableProperty]
    private string _historyExcursionFilter = "All";

    [ObservableProperty]
    private bool _isHistoryFilterAll = true;

    [ObservableProperty]
    private bool _isHistoryFilterLows = false;

    [ObservableProperty]
    private bool _isHistoryFilterHighs = false;

    [ObservableProperty]
    private bool _isHistoryFilterTarget = false;

    [ObservableProperty]
    private int _historyAllCount;

    [ObservableProperty]
    private int _historyLowsCount;

    [ObservableProperty]
    private int _historyHighsCount;

    [ObservableProperty]
    private int _historyTargetCount;

    private readonly List<HistoryReadingItem> _allHistoryReadings = new();

    [ObservableProperty]
    private ObservableCollection<HistoryReadingItem> _historyReadings = new();

    [ObservableProperty]
    private string _historyReadingCountText = "No readings";

    [ObservableProperty]
    private bool _hasHistoryReadings;

    [ObservableProperty]
    private bool _isRefreshingHistory;

    [ObservableProperty]
    private bool _isRefreshingDashboard;

    private bool _isShowingReconnectPrompt;

    [ObservableProperty]
    private string _historyEmptyMessage = "Pull down to load readings";

    public bool HasNoHistoryReadings => !HasHistoryReadings;

    partial void OnHasHistoryReadingsChanged(bool value) =>
        OnPropertyChanged(nameof(HasNoHistoryReadings));

    // Reports Filters & State
    [ObservableProperty]
    private string _activeReportFilter = "7D";

    [ObservableProperty]
    private bool _isReportToday = false;

    [ObservableProperty]
    private bool _isReport7D = true;

    [ObservableProperty]
    private bool _isReport30D = false;

    [ObservableProperty]
    private bool _isReport90D = false;

    [ObservableProperty]
    private bool _isReportCustom = false;

    [ObservableProperty]
    private bool _isCustomReportDialogVisible = false;

    [ObservableProperty]
    private DateTime _customReportStartDate = DateTime.Today.AddDays(-7);

    [ObservableProperty]
    private DateTime _customReportEndDate = DateTime.Today;

    [ObservableProperty]
    private string _reportDateRangeText = $"{DateTime.Today.AddDays(-6):MMM dd} – {DateTime.Today:MMM dd, yyyy}";

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

    // 5-Tier Clinical Consensus AGP Breakdown (ADA / EASD)
    [ObservableProperty]
    private string _reportVeryLowPercent = "1%";

    [ObservableProperty]
    private string _reportLowPercent = "3%";

    [ObservableProperty]
    private string _reportInTargetPercent = "82%";

    [ObservableProperty]
    private string _reportHighPercent = "11%";

    [ObservableProperty]
    private string _reportVeryHighPercent = "3%";

    [ObservableProperty]
    private ColumnDefinitionCollection _reportAgpColumns = CreateAgpColumns(1, 3, 82, 11, 3);

    private static ColumnDefinitionCollection CreateAgpColumns(double vl, double l, double it, double h, double vh) =>
        new()
        {
            new ColumnDefinition(new GridLength(Math.Max(0.5, vl), GridUnitType.Star)),
            new ColumnDefinition(new GridLength(Math.Max(0.5, l), GridUnitType.Star)),
            new ColumnDefinition(new GridLength(Math.Max(0.5, it), GridUnitType.Star)),
            new ColumnDefinition(new GridLength(Math.Max(0.5, h), GridUnitType.Star)),
            new ColumnDefinition(new GridLength(Math.Max(0.5, vh), GridUnitType.Star))
        };

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

    public ObservableCollection<Alert> VisibleAlerts { get; } = new();
    private IReadOnlyList<Alert> _allAlerts = Array.Empty<Alert>();

    [ObservableProperty]
    private bool _hasGlucoseData;

    // Daily Summary Stats
    [ObservableProperty]
    private string _avgGlucose = "--";

    [ObservableProperty]
    private string _avgDiffText = "↓ 6 vs yesterday";

    [ObservableProperty]
    private string _highestGlucose = "--";

    [ObservableProperty]
    private string _highestTime = "No reading";


    
    [ObservableProperty]
    private Color _currentGlucoseColor = Color.FromArgb("#583295"); // Dark gray default

    [ObservableProperty]
    private Color _glucoseAuraColor = Colors.Transparent;

    [ObservableProperty]
    private string _lowestGlucose = "--";

    [ObservableProperty]
    private string _lowestTime = "No reading";

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
    private string _tirVeryLowPercent = "--";

    [ObservableProperty]
    private string _tirLowPercent = "--";

    [ObservableProperty]
    private string _tirInRangePercent = "--";

    [ObservableProperty]
    private string _tirHighPercent = "--";

    [ObservableProperty]
    private string _tirVeryHighPercent = "--";

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
        RealtimeGlucoseService realtimeGlucoseService,
        IAlertService alertService,
        IPdfReportService? pdfReportService = null,
        IReportService? reportService = null,
        ICriticalAlertEngine? criticalAlertEngine = null,
        CGM.PatientApp.Services.Sync.ISyncService? syncService = null,
        ILocalMeasurementRepository? localRepo = null,
        ISmartDietService? smartDietService = null)
    {
        _authService = authService;
        _profileService = profileService;
        _deviceService = deviceService;
        _cgmDeviceService = cgmDeviceService;
        _glucoseService = glucoseService;
        _realtimeGlucoseService = realtimeGlucoseService;
        _alertService = alertService;
        _pdfReportService = pdfReportService ?? new PdfReportService();
        _reportService = reportService;
        _criticalAlertEngine = criticalAlertEngine;
        _syncService = syncService;
        _localRepo = localRepo;
        _smartDietService = smartDietService;
        Title = "GlucoTrack Dashboard";

        InitializeWeeklyCalendar();

        if (Application.Current != null)
        {
            Application.Current.RequestedThemeChanged += (s, e) =>
            {
                ApplyConnectionBadgeColors(IsDeviceConnected);
            };
        }

        _realtimeGlucoseService.GlucoseReadingReceived += (s, e) =>
        {
            SafeAsync.Run(RefreshGlucoseSummaryAsync, "SignalR.RefreshGlucoseSummary");
        };
    }

    public void Activate()
    {
        if (_isActive) return;
        _cgmDeviceService.RawMeasurementReceived += OnRawMeasurementReceived;
        _cgmDeviceService.ConnectionStateChanged += OnCgmConnectionStateChanged;
        _alertService.AlertTriggered += OnAlertTriggered;
        _isActive = true;
    }

    public void Deactivate()
    {
        if (!_isActive) return;
        _cgmDeviceService.RawMeasurementReceived -= OnRawMeasurementReceived;
        _cgmDeviceService.ConnectionStateChanged -= OnCgmConnectionStateChanged;
        _alertService.AlertTriggered -= OnAlertTriggered;
        _isActive = false;
    }

    private void OnAlertTriggered(object? sender, Alert alert)
    {
        Application.Current?.Dispatcher.Dispatch(() =>
        {
            _allAlerts = _allAlerts.Where(existing => existing.Id != alert.Id)
                .Prepend(alert)
                .ToList();
            ApplyAlertFilter();
        });
    }

    public void Dispose()
    {
        _ruleOf15Timer?.Stop();
        StopBackgroundWork();
        Deactivate();
        _criticalAlertEngine?.DismissAlarm();
        GC.SuppressFinalize(this);
    }

    private void OnRawMeasurementReceived(object? sender, CgmRawMeasurement m)
    {
        Application.Current?.Dispatcher.Dispatch(() => SafeAsync.Run(async () =>
        {
            ApplyConnectionState(CgmConnectionState.Ready);
            _latestGlucoseMgDl = m.GlucoseValueMgDl;
            CurrentGlucose = ConvertMgDlForDisplay(m.GlucoseValueMgDl);
            HasGlucoseData = true;
            LastUpdatedText = "Updated just now";
            await UpdateTrendChartAsync(ActiveTrendFilter ?? "3H", new GlucoseMeasurement
            {
                SequenceNumber = m.SequenceNumber,
                GlucoseValue = m.GlucoseValueMgDl,
                MeasurementTime = m.ReceivedTime
            });

            if (SelectedCalendarDay?.Date.Date == DateTime.Today)
            {
                var bg = m.GlucoseValueMgDl < 90 ? Color.FromArgb("#FFFFFF") : m.GlucoseValueMgDl > 130 ? Color.FromArgb("#FFFFFF") : Color.FromArgb("#FFFFFF");
                var fg = m.GlucoseValueMgDl < 90 ? Color.FromArgb("#01B4F1") : m.GlucoseValueMgDl > 130 ? Color.FromArgb("#583295") : Color.FromArgb("#583295");
                var st = m.GlucoseValueMgDl < 90 ? "Low" : m.GlucoseValueMgDl > 130 ? "High" : "In range";
                _allHistoryReadings.Insert(0, new(
                    m.ReceivedTime.ToLocalTime().ToString("h:mm tt"),
                    m.GlucoseValueMgDl.ToString("0"),
                    st, bg, fg, "→", fg));
                ApplyHistoryExcursionFilter();
            }

            if (m.GlucoseValueMgDl < 55)
            {
                TriggerHaptic();
                _criticalAlertEngine?.TriggerHypoEmergencyAlarm(m.GlucoseValueMgDl);
                await Shell.Current.DisplayAlert("🚨 URGENT LOW (HYPOGLYCEMIA)", $"Your glucose is critically low ({m.GlucoseValueMgDl:F0} mg/dL). Treat immediately with 15g fast-acting sugar.", "Dismiss Alarm");
                _criticalAlertEngine?.DismissAlarm();
            }
            else if (m.GlucoseValueMgDl > 250)
            {
                TriggerHaptic();
                await Shell.Current.DisplayAlert("⚠️ HIGH GLUCOSE", $"Your glucose is very high ({m.GlucoseValueMgDl:F0} mg/dL). Consider checking ketones or taking insulin.", "Acknowledge");
            }
        }, "Dashboard.RawMeasurementReceived"));
    }

    private void OnCgmConnectionStateChanged(object? sender, CgmConnectionState state) =>
        Application.Current?.Dispatcher.Dispatch(() => ApplyConnectionState(state));

    [RelayCommand]
    public async Task InitializeAsync()
    {
        await _realtimeGlucoseService.StartAsync();

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
        await LoadReadingsForDateAsync(SelectedCalendarDay?.Date ?? DateTime.Today, 1, false);
        await LoadAlertsAsync();

        if (string.IsNullOrEmpty(SelectedTab))
        {
            SelectTab("Home");
        }

        await UpdateTrendChartAsync(ActiveTrendFilter ?? "3H");
        if (string.Equals(Environment.GetEnvironmentVariable("CGM_USE_MOCK_SERVICES"), "true", StringComparison.OrdinalIgnoreCase))
            StartLiveSimulationTimer();

        await UpdateLastSyncTimeAsync();
    }

    private async Task UpdateLastSyncTimeAsync()
    {
        if (_localRepo != null)
        {
            try
            {
                var recent = await _localRepo.GetRecentMeasurementsAsync(50);
                var lastSynced = recent.Where(m => m.SyncStatus == "Synced" && m.SyncedAt.HasValue).OrderByDescending(m => m.SyncedAt).FirstOrDefault();
                if (lastSynced != null)
                {
                    LastSyncTime = lastSynced.SyncedAt.Value.ToLocalTime().ToString("h:mm tt");
                    LastSyncDate = lastSynced.SyncedAt.Value.ToLocalTime().ToString("MMM dd, yyyy");
                }
            }
            catch { }
        }
    }

    private async Task RefreshDeviceStateAsync()
    {
        // The live BLE transport is authoritative. A backend device record may not
        // exist yet even though verified telemetry is already being received.
        if (IsLiveConnection(_cgmDeviceService.State))
        {
            ApplyConnectionState(_cgmDeviceService.State);
            return;
        }

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
            ApplyConnectionBadgeColors(false);
            StatusBadgeIcon = "\uF127"; // FaLinkSlash
            return;
        }

        IsDeviceConnected = false;
        IsDeviceDisconnected = true;
        ApplyConnectionBadgeColors(false);
        StatusBadgeIcon = "\uF127"; // FaLinkSlash
        DeviceStatusText = "Disconnected";
        BatteryPercent = "--";
        SensorStatus = "Disconnected";
        SensorDaysLeft = "14 days left";
        LastUpdatedText = device.LastCommunicationTime.HasValue
            ? $"Updated {FormatAge(DateTime.UtcNow - device.LastCommunicationTime.Value)}"
            : "Disconnected";

        if (_isShowingReconnectPrompt) return;
        _isShowingReconnectPrompt = true;

        Application.Current?.Dispatcher.Dispatch(() => SafeAsync.Run(async () =>
        {
            try
            {
                DeviceStatusText = "Reconnecting...";
                int retryCount = 0;
                while (retryCount < 3)
                {
                    try
                    {
                        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                        var verifiedDevice = await Task.Run(() => _cgmDeviceService.ConnectAndVerifyAsync(device.BluetoothId, device.DeviceName, timeout.Token));
                        if (verifiedDevice != null)
                        {
                            ApplyConnectionState(CgmConnectionState.Ready);
                            return; // Auto-reconnect succeeded
                        }
                    }
                    catch { }
                    
                    retryCount++;
                    if (retryCount < 3) await Task.Delay(3000);
                }
                DeviceStatusText = "Disconnected";
            }
            finally
            {
                _isShowingReconnectPrompt = false;
            }
        }, "Dashboard.AutoReconnect"));
    }

    private static bool IsCurrentThemeDark => Application.Current?.RequestedTheme == AppTheme.Dark;
    private static Color ConnectedBadgeBg => Color.FromArgb(IsCurrentThemeDark ? "#583295" : "#FFFFFF");
    private static Color ConnectedBadgeText => Color.FromArgb(IsCurrentThemeDark ? "#01B4F1" : "#583295");
    private static Color DisconnectedBadgeBg => Color.FromArgb(IsCurrentThemeDark ? "#583295" : "#FFFFFF");
    private static Color DisconnectedBadgeText => Color.FromArgb(IsCurrentThemeDark ? "#FFFFFF" : "#583295");

    private void ApplyConnectionBadgeColors(bool connected)
    {
        StatusBadgeBgColor = connected ? ConnectedBadgeBg : DisconnectedBadgeBg;
        StatusBadgeTextColor = connected ? ConnectedBadgeText : DisconnectedBadgeText;
    }

    private static bool IsLiveConnection(CgmConnectionState state) => state is
        CgmConnectionState.Connected or CgmConnectionState.DiscoveringServices or
        CgmConnectionState.Ready or CgmConnectionState.Synchronizing;

    private void ApplyConnectionState(CgmConnectionState state)
    {
        var connected = IsLiveConnection(state);
        IsDeviceConnected = connected;
        IsDeviceDisconnected = !connected;
        ApplyConnectionBadgeColors(connected);
        StatusBadgeIcon = connected ? "\uF0C1" : "\uF127";
        DeviceStatusText = connected ? (state == CgmConnectionState.Ready ? "Connected" : state.ToString()) : "Not connected";
        if (connected)
        {
            SensorStatus = "Active";
            LastUpdatedText = "Updated just now";
            var liveDevice = _cgmDeviceService.ConnectedDevice;
            if (liveDevice != null) BatteryPercent = $"{liveDevice.BatteryVoltageMv} mV";
        }
    }

    private string ConvertMgDlForDisplay(double mgDl)
    {
        var unit = string.Equals(GlucoseUnit, "mmol/L", StringComparison.OrdinalIgnoreCase)
            ? Enums.GlucoseUnit.MmolL : Enums.GlucoseUnit.MgDl;
        return GlucoseUnitConverter.Convert(mgDl, Enums.GlucoseUnit.MgDl, unit)
            .ToString(unit == Enums.GlucoseUnit.MmolL ? "0.0" : "0");
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
                _liveSimulationTimer.Interval = TimeSpan.FromMinutes(4);
                _liveSimulationTimer.Tick += (s, e) => SafeAsync.Run(async () =>
                {
                    if (int.TryParse(CurrentGlucose, out var currentVal))
                    {
                        var delta = Random.Shared.Next(-2, 3);
                        var newVal = Math.Clamp(currentVal + delta, 72, 168);
                        CurrentGlucose = newVal.ToString();
                        LastUpdatedText = "Updated just now";

                        await UpdateTrendChartAsync(ActiveTrendFilter ?? "3H", new GlucoseMeasurement
                        {
                            GlucoseValue = newVal,
                            MeasurementTime = DateTime.UtcNow
                        });
                    }
                }, "Dashboard.LiveSimulationTimer");
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
                if (_latestGlucoseMgDl.HasValue) CurrentGlucose = ConvertMgDlForDisplay(_latestGlucoseMgDl.Value);
            }
            else
            {
                GlucoseUnit = "mg/dL";
                if (_latestGlucoseMgDl.HasValue) CurrentGlucose = ConvertMgDlForDisplay(_latestGlucoseMgDl.Value);
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
                HasGlucoseData = true;
                _latestGlucoseMgDl = summary.LatestReading.Unit == Enums.GlucoseUnit.MmolL
                    ? GlucoseUnitConverter.Convert(summary.LatestReading.GlucoseValue, Enums.GlucoseUnit.MmolL, Enums.GlucoseUnit.MgDl)
                    : summary.LatestReading.GlucoseValue;
                CurrentGlucose = ConvertMgDlForDisplay(_latestGlucoseMgDl.Value);
                LastUpdatedText = $"Updated {FormatAge(DateTime.UtcNow - summary.LatestReading.MeasurementTime)}";
            }
            else
            {
                HasGlucoseData = false;
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
            HasGlucoseData = false;
            CurrentGlucose = "--";
            LastUpdatedText = "Error fetching data";
        }
    }

    [RelayCommand]
    private async Task SetTrendFilterAsync(string filter)
    {
        TriggerHaptic();
        await UpdateTrendChartAsync(filter);
    }

    private async Task UpdateTrendChartAsync(string period, GlucoseMeasurement? liveReading = null)
    {
        ActiveTrendFilter = period;
        IsTrend3H = period == "3H";
        IsTrend6H = period == "6H";
        IsTrend12H = period == "12H";
        IsTrend24H = period == "24H";

        var hours = period switch { "6H" => 6, "12H" => 12, "24H" => 24, _ => 3 };
        var now = DateTime.Now;
        var from = now.AddHours(-hours);
        TrendTime1 = from.ToString("h:mm tt");
        TrendTime2 = from.AddHours(hours / 3.0).ToString("h:mm tt");
        TrendTime3 = from.AddHours(hours * 2.0 / 3.0).ToString("h:mm tt");
        TrendTime4 = now.ToString("h:mm tt");

        var readings = (await _glucoseService.GetRecentReadingsAsync(TimeSpan.FromHours(hours)))
            .Where(x => x.GlucoseValue > 0 && x.MeasurementTime.ToLocalTime() >= from)
            .OrderBy(x => x.MeasurementTime)
            .ToList();

        if (liveReading is { GlucoseValue: > 0 } &&
            readings.All(x => x.SequenceNumber != liveReading.SequenceNumber || liveReading.SequenceNumber == 0))
            readings.Add(liveReading);

        if (readings.Count == 0)
        {
            UpdateDummyTrendChart(period);
            return;
        }

        TrendWindowAverage = $"{readings.Average(x => x.GlucoseValue):0} mg/dL";
        TrendWindowMin = $"{readings.Min(x => x.GlucoseValue):0}";
        TrendWindowMax = $"{readings.Max(x => x.GlucoseValue):0}";
        var inRangeCount = readings.Count(x => x.GlucoseValue >= 70 && x.GlucoseValue <= 180);
        TrendWindowTir = $"{inRangeCount * 100.0 / readings.Count:0}%";

        const double minVal = 40;
        var observablePoints = readings.Select(r => new DateTimePoint(r.MeasurementTime.ToLocalTime(), r.GlucoseValue)).ToList();

        var latest = readings.Last();
        var colorHex = latest.GlucoseValue switch
        {
            > 250 or < 55 => "#583295",
            > 180 => "#583295",
            < 70 => "#01B4F1",
            _ => "#583295"
        };
        var skColor = SKColor.Parse(colorHex);

        TrendSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = observablePoints,
                Fill = null,
                Stroke = new SolidColorPaint(skColor) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        // Update XAxis limits to reflect the current window precisely
        var xAxis = TrendXAxes[0];
        xAxis.MinLimit = from.Ticks;
        xAxis.MaxLimit = now.Ticks;
        TrendXAxes = new[] { xAxis };
    }

    private void UpdateDummyTrendChart(string period, int? overrideLatest = null)
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

        TrendWindowAverage = $"{values.Average():0} mg/dL";
        TrendWindowMin = $"{values.Min():0}";
        TrendWindowMax = $"{values.Max():0}";
        var inRangeDummy = values.Count(x => x >= 70 && x <= 180);
        TrendWindowTir = $"{inRangeDummy * 100.0 / values.Count:0}%";

        var hoursNum = period switch { "6H" => 6, "12H" => 12, "24H" => 24, _ => 3 };

        var observablePoints = new List<DateTimePoint>();
        for (int i = 0; i < values.Count; i++)
        {
            var timeOffset = (values.Count - 1 - i) * (hoursNum * 60.0 / values.Count);
            observablePoints.Add(new DateTimePoint(now.AddMinutes(-timeOffset), values[i]));
        }

        var lastVal = values.Last();
        var colorHex = lastVal switch
        {
            > 250 or < 55 => "#583295",
            > 180 => "#583295",
            < 70 => "#01B4F1",
            _ => "#583295"
        };
        var skColor = SKColor.Parse(colorHex);

        TrendSeries = new ISeries[]
        {
            new LineSeries<DateTimePoint>
            {
                Values = observablePoints,
                Fill = null,
                Stroke = new SolidColorPaint(skColor) { StrokeThickness = 3 },
                GeometrySize = 0,
                LineSmoothness = 0.5
            }
        };

        var xAxis = TrendXAxes[0];
        xAxis.MinLimit = now.AddHours(-hoursNum).Ticks;
        xAxis.MaxLimit = now.Ticks;
        TrendXAxes = new[] { xAxis };

        if (lastVal > 250)
        {
            TrendStatus = "High ↑";
            CurrentGlucoseColor = Color.FromArgb("#583295");
            GlucoseAuraColor = Color.FromArgb("#583295"); 
        }
        else if (lastVal > 180)
        {
            TrendStatus = "High ↗";
            CurrentGlucoseColor = Color.FromArgb("#583295");
            GlucoseAuraColor = Color.FromArgb("#583295"); 
        }
        else if (lastVal < 55)
        {
            TrendStatus = "Low ↓";
            CurrentGlucoseColor = Color.FromArgb("#583295");
            GlucoseAuraColor = Color.FromArgb("#583295"); 
        }
        else if (lastVal < 70)
        {
            TrendStatus = "Low ↘";
            CurrentGlucoseColor = Color.FromArgb("#01B4F1");
            GlucoseAuraColor = Color.FromArgb("#01B4F1");
        }
        else
        {
            TrendStatus = "Stable →";
            CurrentGlucoseColor = Color.FromArgb("#583295"); 
            GlucoseAuraColor = Color.FromArgb("#583295"); 
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
    private void SetHistoryExcursionFilter(string filter)
    {
        TriggerHaptic();
        HistoryExcursionFilter = filter;
        IsHistoryFilterAll = filter == "All";
        IsHistoryFilterLows = filter == "Lows";
        IsHistoryFilterHighs = filter == "Highs";
        IsHistoryFilterTarget = filter == "Target";
        ApplyHistoryExcursionFilter();
    }

    private void ApplyHistoryExcursionFilter()
    {
        HistoryAllCount = _allHistoryReadings.Count;
        HistoryLowsCount = _allHistoryReadings.Count(x => double.TryParse(x.ValueText, out var v) && v < 70);
        HistoryHighsCount = _allHistoryReadings.Count(x => double.TryParse(x.ValueText, out var v) && v > 180);
        HistoryTargetCount = _allHistoryReadings.Count(x => double.TryParse(x.ValueText, out var v) && v >= 70 && v <= 180);

        IEnumerable<HistoryReadingItem> filtered = HistoryExcursionFilter switch
        {
            "Lows" => _allHistoryReadings.Where(x => double.TryParse(x.ValueText, out var v) && v < 70),
            "Highs" => _allHistoryReadings.Where(x => double.TryParse(x.ValueText, out var v) && v > 180),
            "Target" => _allHistoryReadings.Where(x => double.TryParse(x.ValueText, out var v) && v >= 70 && v <= 180),
            _ => _allHistoryReadings
        };

        HistoryReadings.Clear();
        foreach (var item in filtered)
        {
            HistoryReadings.Add(item);
        }

        HasHistoryReadings = HistoryReadings.Count > 0;
        HistoryReadingCountText = HistoryExcursionFilter == "All"
            ? (HistoryReadings.Count == 1 ? "1 reading" : $"{HistoryReadings.Count} readings")
            : $"{HistoryReadings.Count} of {HistoryAllCount} readings";

        HistoryEmptyMessage = HistoryExcursionFilter switch
        {
            "Lows" => "No low readings (<90 mg/dL)",
            "Highs" => "No high readings (>130 mg/dL)",
            "Target" => "No in-target readings (90–130 mg/dL)",
            _ => "No readings for this date"
        };
    }

    [RelayCommand]
    private async Task SetHistoryFilterAsync(string filter)
    {
        TriggerHaptic();
        ActiveHistoryFilter = filter;
        IsHistoryToday = filter == "Today";
        IsHistory7D = filter == "7D";
        IsHistory14D = filter == "14D";
        IsHistory30D = filter == "30D";

        await LoadHistoryReadingsAsync(filter);
    }

    private async Task LoadHistoryReadingsAsync(string filter)
    {
        _allHistoryReadings.Clear();
        var range = filter switch
        {
            "7D" => TimeSpan.FromDays(7),
            "14D" => TimeSpan.FromDays(14),
            "30D" => TimeSpan.FromDays(30),
            _ => TimeSpan.FromDays(1)
        };

        var readings = await _glucoseService.GetRecentReadingsAsync(range);
        foreach (var reading in readings.OrderByDescending(x => x.MeasurementTime).Take(100))
        {
            var (background, foreground) = reading.Status switch
            {
                GlucoseStatus.Low => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#01B4F1")),
                GlucoseStatus.UrgentLow or GlucoseStatus.UrgentHigh => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#583295")),
                GlucoseStatus.High => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#583295")),
                _ => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#583295"))
            };
            var arrow = reading.Trend switch
            {
                GlucoseTrend.RisingRapidly => "↑",
                GlucoseTrend.Rising => "↗",
                GlucoseTrend.Falling => "↘",
                GlucoseTrend.FallingRapidly => "↓",
                _ => "→"
            };
            _allHistoryReadings.Add(new(
                filter == "Today"
                    ? reading.MeasurementTime.ToLocalTime().ToString("h:mm tt")
                    : reading.MeasurementTime.ToLocalTime().ToString("ddd, MMM d h:mm tt"),
                reading.GlucoseValue.ToString("0"),
                reading.Status == GlucoseStatus.Normal ? "In range" : reading.Status.ToString(),
                background, foreground, arrow, foreground));
        }
        ApplyHistoryExcursionFilter();
    }

    private void PopulateHistoryReadings(string filter)
    {
        _allHistoryReadings.Clear();

        var greenBg = Color.FromArgb("#FFFFFF");
        var greenText = Color.FromArgb("#583295");
        var blueBg = Color.FromArgb("#FFFFFF");
        var blueText = Color.FromArgb("#01B4F1");
        var amberBg = Color.FromArgb("#FFFFFF");
        var amberText = Color.FromArgb("#583295");
        var redBg = Color.FromArgb("#FFFFFF");
        var redText = Color.FromArgb("#583295");

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

                _allHistoryReadings.Add(new("Wed, May 8", "92", "Optimal", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Tue, May 7", "96", "Optimal", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Mon, May 6", "93", "In Range", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Sun, May 5", "97", "Optimal", greenBg, greenText, "↗", greenText));
                _allHistoryReadings.Add(new("Sat, May 4", "95", "Optimal", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Fri, May 3", "98", "In Range", greenBg, greenText, "↗", greenText));
                _allHistoryReadings.Add(new("Thu, May 2", "102", "In Range", greenBg, greenText, "→", greenText));
                break;

            case "14D":
                AvgGlucose = "101";
                AvgDiffText = "↓ 2 vs prev 14D";
                HighestGlucose = "178";
                HighestTime = "Apr 28, 1:45 PM";
                LowestGlucose = "58";
                LowestTime = "May 1, 4:00 AM";
                TimeInRange = "80%";

                _allHistoryReadings.Add(new("Week 2 Avg", "96", "Optimal", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Week 1 Avg", "105", "In Range", greenBg, greenText, "↗", greenText));
                _allHistoryReadings.Add(new("Peak Day (Apr 28)", "132", "Elevated", amberBg, amberText, "↑", amberText));
                _allHistoryReadings.Add(new("Best Day (May 4)", "89", "Optimal", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Overnight Lows", "64", "Low", redBg, redText, "↘", redText));
                break;

            case "30D":
                AvgGlucose = "104";
                AvgDiffText = "↑ 1 vs prev month";
                HighestGlucose = "185";
                HighestTime = "Apr 15, 8:20 PM";
                LowestGlucose = "55";
                LowestTime = "Apr 20, 3:15 AM";
                TimeInRange = "79%";

                _allHistoryReadings.Add(new("Monthly Avg", "104", "In Range", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Week 4 Avg", "96", "Optimal", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Week 3 Avg", "102", "In Range", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("Week 2 Avg", "108", "In Range", greenBg, greenText, "↗", greenText));
                _allHistoryReadings.Add(new("Week 1 Avg", "110", "In Range", greenBg, greenText, "→", greenText));
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

                _allHistoryReadings.Add(new("9:30 AM", "112", "Stable", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("8:30 AM", "105", "Stable", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("7:30 AM", "98", "Stable", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("6:30 AM", "86", "In Range", greenBg, greenText, "↗", greenText));
                _allHistoryReadings.Add(new("5:30 AM", "78", "In Range", greenBg, greenText, "→", greenText));
                _allHistoryReadings.Add(new("4:30 AM", "68", "Low", redBg, redText, "↘", redText));
                break;
        }

        ApplyHistoryExcursionFilter();
    }

    [RelayCommand]
    private void OpenCustomReportDialog()
    {
        IsCustomReportDialogVisible = true;
    }

    [RelayCommand]
    private void CloseCustomReportDialog()
    {
        IsCustomReportDialogVisible = false;
    }

    [RelayCommand]
    private async Task ApplyCustomReportFilterAsync()
    {
        IsCustomReportDialogVisible = false;
        ActiveReportFilter = "Custom";
        IsReportToday = false;
        IsReport7D = false;
        IsReport30D = false;
        IsReport90D = false;
        IsReportCustom = true;

        var start = CustomReportStartDate.Date;
        var end = CustomReportEndDate.Date;
        if (start > end) (start, end) = (end, start);

        ReportDateRangeText = $"{start:MMM dd, yyyy} – {end:MMM dd, yyyy}";

        if (_reportService != null)
        {
            try
            {
                var summary = await _reportService.GetDetailedReportAsync(null, start, end);
                if (summary != null && summary.TotalReadings > 0)
                {
                    ReportAvgGlucose = summary.AvgGlucose;
                    ReportHighest = summary.HighestGlucose;
                    ReportLowest = summary.LowestGlucose;
                    ReportTimeInRange = summary.TimeInRange;
                    ReportEstimatedA1c = summary.EstimatedA1c;
                    ReportGlucoseVariability = summary.GlucoseVariability;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load custom report: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private async Task SetReportFilterAsync(string filter)
    {
        TriggerHaptic();
        ActiveReportFilter = filter;
        IsReportToday = filter == "Today" || filter == "1D";
        IsReport7D = filter == "7D";
        IsReport30D = filter == "30D";
        IsReport90D = filter == "90D";
        IsReportCustom = filter == "Custom";

        if (filter == "Custom")
        {
            IsCustomReportDialogVisible = true;
            return;
        }

        DateTime startDate;
        DateTime endDate = DateTime.Today;

        switch (filter)
        {
            case "Today":
            case "1D":
                startDate = DateTime.Today;
                ReportDateRangeText = $"{DateTime.Today:MMM dd, yyyy}";
                ReportAvgDiffText = "Today's summary";
                break;

            case "30D":
                startDate = DateTime.Today.AddDays(-29);
                ReportDateRangeText = $"{startDate:MMM dd} – {endDate:MMM dd, yyyy}";
                ReportAvgDiffText = "30-day summary";
                break;

            case "90D":
                startDate = DateTime.Today.AddDays(-89);
                ReportDateRangeText = $"{startDate:MMM dd} – {endDate:MMM dd, yyyy}";
                ReportAvgDiffText = "90-day summary";
                break;

            case "7D":
            default:
                startDate = DateTime.Today.AddDays(-6);
                ReportDateRangeText = $"{startDate:MMM dd} – {endDate:MMM dd, yyyy}";
                ReportAvgDiffText = "7-day summary";
                break;
        }

        if (_reportService != null)
        {
            try
            {
                var summary = await _reportService.GetDetailedReportAsync(null, startDate, endDate);
                if (summary != null && summary.TotalReadings > 0)
                {
                    ReportAvgGlucose = summary.AvgGlucose;
                    ReportHighest = summary.HighestGlucose;
                    ReportLowest = summary.LowestGlucose;
                    ReportTimeInRange = summary.TimeInRange;
                    ReportEstimatedA1c = summary.EstimatedA1c;
                    ReportGlucoseVariability = summary.GlucoseVariability;

                    var totalHigh = summary.TarPercentage + summary.VeryHighPercentage;
                    var totalLow = summary.TbrPercentage + summary.VeryLowPercentage;

                    ReportPieSeries = new ISeries[]
                    {
                        new PieSeries<double> { Values = new double[] { summary.TirPercentage }, Name = "In Range", Fill = new SolidColorPaint(SKColor.Parse("#01B4F1")), InnerRadius = 40 },
                        new PieSeries<double> { Values = new double[] { totalHigh }, Name = "High", Fill = new SolidColorPaint(SKColor.Parse("#583295")), InnerRadius = 40 },
                        new PieSeries<double> { Values = new double[] { totalLow }, Name = "Low", Fill = new SolidColorPaint(SKColor.Parse("#F43F5E")), InnerRadius = 40 }
                    };

                    var trendValues = summary.DailySummaries
                        .Select(d => double.TryParse(d.AvgGlucose, out double val) ? val : 0)
                        .Where(v => v > 0)
                        .ToArray();

                    if (trendValues.Length > 0)
                    {
                        ReportTrendSeries = new ISeries[]
                        {
                            new LineSeries<double>
                            {
                                Values = trendValues,
                                Fill = null,
                                Stroke = new SolidColorPaint(SKColor.Parse("#01B4F1")) { StrokeThickness = 3 },
                                GeometryFill = new SolidColorPaint(SKColor.Parse("#01B4F1")),
                                GeometryStroke = new SolidColorPaint(SKColor.Parse("#FFFFFF")) { StrokeThickness = 2 },
                                GeometrySize = 10
                            }
                        };
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load report summary: {ex.Message}");
            }
        }
    }

    [RelayCommand]
    private void SetAlertFilter(string filter)
    {
        TriggerHaptic();
        ActiveAlertFilter = filter;
        IsAlertInfo = string.Equals(filter, "Info", StringComparison.OrdinalIgnoreCase);
        IsAlertAll = filter == "All";
        IsAlertCritical = filter == "Critical";
        IsAlertUnread = filter == "Unread";
        ApplyAlertFilter();
        IsAlertInfo = filter == "Info" || filter == "ℹ️ Info";
    }

    private async Task LoadAlertsAsync()
    {
        _allAlerts = await _alertService.GetAlertsAsync();
        ApplyAlertFilter();
    }

    private void ApplyAlertFilter()
    {
        IEnumerable<Alert> alerts = _allAlerts;

        if (IsAlertCritical)
            alerts = alerts.Where(alert => alert.IsCritical);
        else if (IsAlertUnread)
            alerts = alerts.Where(alert => !alert.IsRead);
        else if (IsAlertInfo)
            alerts = alerts.Where(alert => !alert.IsCritical);

        VisibleAlerts.Clear();
        foreach (var alert in alerts.OrderByDescending(alert => alert.Timestamp))
            VisibleAlerts.Add(alert);
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
    private async Task OpenAlertAsync(string alert)
    {
        var matched = _allAlerts.FirstOrDefault(a => a.Id == alert || a.Title == alert || alert.Contains(a.Title));
        if (matched != null)
        {
            await _alertService.MarkAlertAsReadAsync(matched.Id);
            matched.IsRead = true;
            ApplyAlertFilter();
        }
        await Shell.Current.GoToAsync($"AppDetailPage?section=Alert&item={Uri.EscapeDataString(alert)}");
    }

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
            PdfReportSummary? summary = null;

            if (_reportService != null)
            {
                try
                {
                    DateTime start, end = DateTime.UtcNow.Date;
                    if (IsReportCustom)
                    {
                        start = CustomReportStartDate.Date;
                        end = CustomReportEndDate.Date;
                        if (start > end) (start, end) = (end, start);
                    }
                    else if (IsReportToday)
                    {
                        start = DateTime.UtcNow.Date;
                        end = DateTime.UtcNow.Date;
                    }
                    else
                    {
                        var days = IsReport30D ? 30 : (IsReport90D ? 90 : 7);
                        start = DateTime.UtcNow.Date.AddDays(-days + 1);
                    }
                    summary = await _reportService.GetDetailedReportAsync(null, start, end);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"API report fetch failed, using local metrics: {ex.Message}");
                }
            }

            string filePath;
            if (summary != null && summary.TotalReadings > 0)
            {
                filePath = await pdfService.GenerateReportPdfAsync(summary);
            }
            else
            {
                filePath = await pdfService.GenerateReportPdfAsync(
                    patientName: string.IsNullOrWhiteSpace(UserFullName) ? "Patient" : UserFullName,
                    patientEmail: UserEmail,
                    dateRange: ReportDateRangeText,
                    avgGlucose: ReportAvgGlucose,
                    timeInRange: ReportTimeInRange,
                    highestGlucose: ReportHighest,
                    lowestGlucose: ReportLowest,
                    estimatedA1c: ReportEstimatedA1c,
                    glucoseVariability: ReportGlucoseVariability
                );
            }

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

        if (IsHistorySelected)
        {
            SafeAsync.Run(() => LoadReadingsForDateAsync(SelectedCalendarDay?.Date ?? DateTime.Today, 1, false), "Dashboard.SelectTab.History");
        }

        if (IsAlertsSelected)
        {
            SafeAsync.Run(LoadAlertsAsync, "Dashboard.SelectTab.Alerts");
        }

        // Dynamically refresh user and profile data
        SafeAsync.Run(RefreshUserDataAsync, "Dashboard.SelectTab.RefreshUserData");
    }

    [RelayCommand]
    private async Task NavigateToPairingAsync()
    {
        TriggerHaptic();
        await Shell.Current.GoToAsync("//DeviceSelectionPage");
    }

    [RelayCommand]
    private async Task NavigateToFamilyAsync() => await Shell.Current.GoToAsync("FamilyPage");

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
        if (string.IsNullOrWhiteSpace(NewPassword) || NewPassword.Length < 8)
        {
            SecurityErrorMessage = "New password must be at least 8 characters.";
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

    // ================= EMERGENCY HYPOGLYCEMIA "RULE OF 15" COMMANDS =================
    [RelayCommand]
    private void StartRuleOf15Timer()
    {
        TriggerHaptic();
        _ruleOf15Timer?.Stop();
        RuleOf15SecondsRemaining = 900;
        RuleOf15TimerText = "15:00";
        RuleOf15Progress = 1.0;
        IsRuleOf15Active = true;
        RuleOf15Status = "Timer running: Rest & re-check in 15 mins";

        _ruleOf15Timer = Application.Current?.Dispatcher.CreateTimer();
        if (_ruleOf15Timer != null)
        {
            _ruleOf15Timer.Interval = TimeSpan.FromSeconds(1);
            _ruleOf15Timer.Tick += (s, e) =>
            {
                RuleOf15SecondsRemaining--;
                if (RuleOf15SecondsRemaining <= 0)
                {
                    _ruleOf15Timer.Stop();
                    RuleOf15TimerText = "00:00";
                    RuleOf15Progress = 0.0;
                    IsRuleOf15Active = false;
                    RuleOf15Status = "⚠️ 15 min elapsed: Re-check blood glucose now!";
                    TriggerHaptic();
                    _ = Shell.Current.DisplayAlert("⏱️ Rule of 15 Complete", "15 minutes have passed since your carb intake. Please check your glucose now. If it remains below 70 mg/dL, take another 15g carbs.", "Understood");
                }
                else
                {
                    var mins = RuleOf15SecondsRemaining / 60;
                    var secs = RuleOf15SecondsRemaining % 60;
                    RuleOf15TimerText = $"{mins:00}:{secs:00}";
                    RuleOf15Progress = (double)RuleOf15SecondsRemaining / 900.0;
                }
            };
            _ruleOf15Timer.Start();
        }
    }

    [RelayCommand]
    private void ResetRuleOf15Timer()
    {
        TriggerHaptic();
        _ruleOf15Timer?.Stop();
        RuleOf15SecondsRemaining = 900;
        RuleOf15TimerText = "15:00";
        RuleOf15Progress = 1.0;
        IsRuleOf15Active = false;
        RuleOf15Status = "Ready: Consume 15g fast-acting carbs";
    }

    [RelayCommand]
    private async Task CallEmergencyCaregiverAsync()
    {
        TriggerHaptic();
        try
        {
            if (PhoneDialer.Default.IsSupported)
            {
                PhoneDialer.Default.Open(EmergencyCaregiverPhone);
            }
            else
            {
                await Shell.Current.DisplayAlert("Emergency Caregiver", $"Calling designated contact: {EmergencyCaregiverPhone}", "OK");
            }
        }
        catch (Exception ex)
        {
            await Shell.Current.DisplayAlert("Caregiver Contact", $"Emergency Caregiver: {EmergencyCaregiverPhone}\n({ex.Message})", "OK");
        }
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
                1 => Color.FromArgb("#583295"), // amber
                4 => Color.FromArgb("#583295"), // green
                _ => Color.FromArgb("#583295")
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
    private async Task PreviousDayAsync()
    {
        if (SelectedCalendarDay == null || WeeklyCalendarDays.Count == 0) return;
        int idx = WeeklyCalendarDays.IndexOf(SelectedCalendarDay);
        if (idx > 0)
        {
            await SelectCalendarDayAsync(WeeklyCalendarDays[idx - 1]);
        }
    }

    [RelayCommand]
    private async Task NextDayAsync()
    {
        if (SelectedCalendarDay == null || WeeklyCalendarDays.Count == 0) return;
        int idx = WeeklyCalendarDays.IndexOf(SelectedCalendarDay);
        if (idx < WeeklyCalendarDays.Count - 1)
        {
            await SelectCalendarDayAsync(WeeklyCalendarDays[idx + 1]);
        }
    }

    [RelayCommand]
    private async Task SelectCalendarDayAsync(CalendarDayItem day)
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

        await LoadReadingsForDateAsync(day.Date, 1, false);
    }

    [RelayCommand]
    private async Task RefreshDashboardAsync()
    {
        // RefreshView sets IsRefreshingDashboard to true before executing the command.
        // If we return here, the finally block is never reached and the spinner spins forever.
        IsRefreshingDashboard = true;
        try
        {
            TriggerHaptic();
            if (IsHistorySelected)
            {
                if (_syncService != null)
                {
                    await _syncService.SyncNowAsync();
                }
                await LoadReadingsForDateAsync(SelectedCalendarDay?.Date ?? DateTime.Today, 1, false);
            }
            else
            {
                await RefreshUserDataAsync();
                await RefreshDeviceStateAsync();
                await RefreshGlucoseSummaryAsync();
                await UpdateTrendChartAsync(ActiveTrendFilter ?? "3H");
                await LoadAlertsAsync();
                await LoadReadingsForDateAsync(SelectedCalendarDay?.Date ?? DateTime.Today, 1, false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] RefreshDashboard failed: {ex}");
        }
        finally
        {
            IsRefreshingDashboard = false;
        }
    }

    [RelayCommand]
    private async Task RefreshHistoryAsync()
    {
        // RefreshView sets IsRefreshing to true before executing the command.
        // If we return here, the finally block is never reached and the spinner spins forever.
        IsRefreshingHistory = true;
        try
        {
            if (_syncService != null)
            {
                await _syncService.SyncNowAsync();
            }
            await LoadReadingsForDateAsync(SelectedCalendarDay?.Date ?? DateTime.Today, 1, false);
        }
        finally { IsRefreshingHistory = false; }
    }

    [RelayCommand]
    private async Task LoadMoreHistoryAsync()
    {
        if (_isLoadingMoreHistory || _historyPage <= 0 || _historyPage >= _historyTotalPages) return;
        _isLoadingMoreHistory = true;
        try { await LoadReadingsForDateAsync(SelectedCalendarDay?.Date ?? DateTime.Today, _historyPage + 1, true); }
        finally { _isLoadingMoreHistory = false; }
    }

    private void ResetHistoryForSelectedDate()
    {
        _allHistoryReadings.Clear();
        HistoryReadings.Clear();
        _historyPage = 0;
        _historyTotalPages = 0;
        HasHistoryReadings = false;
        HistoryReadingCountText = "Pull to refresh";
        HistoryEmptyMessage = "Pull down to load readings";
        AvgGlucose = HighestGlucose = LowestGlucose = "--";
        AvgDiffText = "Pull down to load data";
        HighestTime = LowestTime = "No reading";
        TimeInRange = "--";
        HistoryAllCount = 0;
        HistoryLowsCount = 0;
        HistoryHighsCount = 0;
        HistoryTargetCount = 0;
    }

    private async Task LoadReadingsForDateAsync(DateTime selectedDate, int page, bool append)
    {
        if (!append) _allHistoryReadings.Clear();

        var result = await _glucoseService.GetReadingsForDateAsync(selectedDate, page, HistoryPageSize);
        var selectedReadings = result.Items.ToList();

        // If backend returned no items, check local SQLite cache for unsynced or cached measurements
        if (selectedReadings.Count == 0 && _localRepo != null)
        {
            try
            {
                var localItems = await _localRepo.GetMeasurementsForDateAsync(selectedDate);
                if (localItems.Count > 0)
                {
                    selectedReadings = localItems.Select(m => new GlucoseMeasurement
                    {
                        SequenceNumber = (ushort)Math.Clamp(m.SequenceNumber, ushort.MinValue, ushort.MaxValue),
                        GlucoseValue = m.GlucoseValue,
                        Unit = CGM.PatientApp.Enums.GlucoseUnit.MgDl,
                        MeasurementTime = m.MeasuredAt,
                        Trend = Enum.TryParse<GlucoseTrend>(m.Trend, true, out var t) ? t : GlucoseTrend.Stable,
                        Status = m.GlucoseValue < 70 ? GlucoseStatus.Low : m.GlucoseValue > 180 ? GlucoseStatus.High : GlucoseStatus.Normal
                    }).ToList();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DashboardViewModel] Local fallback failed: {ex}");
            }
        }

        selectedReadings = selectedReadings
            .OrderByDescending(reading => reading.MeasurementTime)
            .ToList();

        foreach (var reading in selectedReadings)
        {
            var (background, foreground) = reading.Status switch
            {
                GlucoseStatus.Low => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#01B4F1")),
                GlucoseStatus.UrgentLow or GlucoseStatus.UrgentHigh => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#583295")),
                GlucoseStatus.High => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#583295")),
                _ => (Color.FromArgb("#FFFFFF"), Color.FromArgb("#583295"))
            };
            var arrow = reading.Trend switch
            {
                GlucoseTrend.RisingRapidly => "↑",
                GlucoseTrend.Rising => "↗",
                GlucoseTrend.Falling => "↘",
                GlucoseTrend.FallingRapidly => "↓",
                _ => "→"
            };

            _allHistoryReadings.Add(new(
                reading.MeasurementTime.ToLocalTime().ToString("h:mm tt"),
                reading.GlucoseValue.ToString("0"),
                reading.Status == GlucoseStatus.Normal ? "In range" : reading.Status.ToString(),
                background, foreground, arrow, foreground));
        }

        ApplyHistoryExcursionFilter();

        _historyPage = result.Page;
        _historyTotalPages = result.TotalPages;

        if (append) return;

        if (_allHistoryReadings.Count == 0)
        {
            AvgGlucose = HighestGlucose = LowestGlucose = "--";
            AvgDiffText = $"No data for {selectedDate:MMM d}";
            HighestTime = LowestTime = "No reading";
            TimeInRange = "--";
            return;
        }

        AvgGlucose = selectedReadings.Average(x => x.GlucoseValue).ToString("0");
        var highest = selectedReadings.MaxBy(x => x.GlucoseValue)!;
        var lowest = selectedReadings.MinBy(x => x.GlucoseValue)!;
        HighestGlucose = highest.GlucoseValue.ToString("0");
        HighestTime = highest.MeasurementTime.ToLocalTime().ToString("h:mm tt");
        LowestGlucose = lowest.GlucoseValue.ToString("0");
        LowestTime = lowest.MeasurementTime.ToLocalTime().ToString("h:mm tt");
        TimeInRange = $"{selectedReadings.Count(x => x.GlucoseValue is >= 70 and <= 180) * 100.0 / selectedReadings.Count:0}%";
        AvgDiffText = $"Average for {selectedDate:MMM d}";
    }

    private void PopulateDayReadings(CalendarDayItem day)
    {
        _allHistoryReadings.Clear();

        var greenBg = Color.FromArgb("#FFFFFF");
        var greenText = Color.FromArgb("#583295");
        var amberBg = Color.FromArgb("#FFFFFF");
        var amberText = Color.FromArgb("#583295");
        var redBg = Color.FromArgb("#FFFFFF");
        var redText = Color.FromArgb("#583295");

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

        _allHistoryReadings.Add(new($"{dayDateStr} 9:00 PM", "108", "Stable", greenBg, greenText, "→", greenText));
        _allHistoryReadings.Add(new($"{dayDateStr} 6:30 PM", "136", "In Range", greenBg, greenText, "↗", greenText));
        _allHistoryReadings.Add(new($"{dayDateStr} 1:40 PM", HighestGlucose, day.TirPercent == "68%" ? "High" : "In Range", day.TirPercent == "68%" ? amberBg : greenBg, day.TirPercent == "68%" ? amberText : greenText, "↑", day.TirPercent == "68%" ? amberText : greenText));
        _allHistoryReadings.Add(new($"{dayDateStr} 12:15 PM", "118", "Stable", greenBg, greenText, "→", greenText));
        _allHistoryReadings.Add(new($"{dayDateStr} 8:00 AM", "95", "Optimal", greenBg, greenText, "→", greenText));
        _allHistoryReadings.Add(new($"{dayDateStr} 4:15 AM", LowestGlucose, LowestGlucose == "62" ? "Low" : "Optimal", LowestGlucose == "62" ? redBg : greenBg, LowestGlucose == "62" ? redText : greenText, LowestGlucose == "62" ? "↘" : "→", LowestGlucose == "62" ? redText : greenText));

        ApplyHistoryExcursionFilter();
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

    [ObservableProperty]
    private ISeries[] _reportPieSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _reportTrendSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _reportTrendXAxes = new Axis[]
    {
        new Axis
        {
            Labeler = value => new DateTime((long)value).ToString("MMM dd"),
            TextSize = 10,
            LabelsPaint = new SolidColorPaint(SKColor.Parse("#583295")),
            MinStep = TimeSpan.FromDays(1).Ticks
        }
    };

    [RelayCommand]
    private async Task ShareDoctorReportAsync()
    {
        TriggerHaptic();
        await ExportReportAsync();
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
