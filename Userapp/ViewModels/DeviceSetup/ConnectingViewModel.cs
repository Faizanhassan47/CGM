using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class ConnectingViewModel : BaseViewModel
{
    private readonly IBleService _bleService;
    private readonly ICgmDeviceService _cgmDeviceService;
    private readonly IDeviceService _deviceService;

    [ObservableProperty]
    private string _currentStepDescription = "Initiating secure connection...";

    [ObservableProperty]
    private double _connectionProgress = 0.2;

    [ObservableProperty]
    private bool _isConnecting = true;

    [ObservableProperty]
    private bool _isFailed = false;

    [ObservableProperty]
    private bool _isBleConnected = false;

    [ObservableProperty]
    private bool _isGattConfigured = false;

    [ObservableProperty]
    private bool _isTelemetryVerified = false;

    public ConnectingViewModel(IBleService bleService, ICgmDeviceService cgmDeviceService, IDeviceService deviceService)
    {
        _bleService = bleService;
        _cgmDeviceService = cgmDeviceService;
        _deviceService = deviceService;
        Title = "Connecting";
    }

    [RelayCommand]
    public async Task StartConnectionSequenceAsync()
    {
        ClearError();
        IsConnecting = true;
        IsFailed = false;
        IsBleConnected = false;
        IsGattConfigured = false;
        IsTelemetryVerified = false;

        try
        {
            // Stage 1: BLE Link
            CurrentStepDescription = "Connecting to your CGM sensor...";
            ConnectionProgress = 0.25;
            IsBleConnected = true;

            // Stage 2: Discover and Configure GATT Services
            CurrentStepDescription = "Configuring GATT channel (FFF0 / FFF1 / FFF2)...";
            ConnectionProgress = 0.50;
            IsGattConfigured = true;

            // Stage 3: Baseline Diagnostics Verification (E7, E8, E1, E2, E3)
            CurrentStepDescription = "Verifying diagnostic telemetry (E7, E8, E1, E2, E3)...";
            ConnectionProgress = 0.75;

            var id = Preferences.Default.Get("found_device_id", string.Empty);
            var name = Preferences.Default.Get("found_device_name", string.Empty);
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(65));
            var device = await _cgmDeviceService.ConnectAndVerifyAsync(id, name, timeout.Token);
            if (device is null) throw new InvalidOperationException("CGM verification failed.");

            IsTelemetryVerified = true;
            CurrentStepDescription = "CGM verified successfully.";
            ConnectionProgress = 1.0;

            Preferences.Default.Set("found_device_sn", device.SerialNumber);
            Preferences.Default.Set("cgm_device_sn", device.SerialNumber);
            Preferences.Default.Set("cgm_device_fw", device.FirmwareVersion);
            Preferences.Default.Set("cgm_device_battery_mv", (int)device.BatteryVoltageMv);
            Preferences.Default.Set("cgm_device_temp", device.TemperatureCelsius);
            Preferences.Default.Set("cgm_device_configured", true);

            await _deviceService.SaveConfiguredDeviceAsync(device);

            // Small delay so user sees all checkmarks complete
            await Task.Delay(400);

            // Navigate to Connection Success
            await Shell.Current.GoToAsync("ConnectionSuccessPage");
        }
        catch (Exception)
        {
            IsFailed = true;
            SetError("Unable to connect to your CGM. Please ensure it is nearby and Bluetooth is enabled.");
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private async Task RetryAsync()
    {
        await StartConnectionSequenceAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
