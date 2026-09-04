using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class ConnectionSuccessViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;

    [ObservableProperty]
    private string _deviceIdentifier = "Ending in C221";

    [ObservableProperty]
    private string _firmwareVersion = "A100";

    [ObservableProperty]
    private string _batteryStatus = "Good (2850 mV)";

    [ObservableProperty]
    private string _sensorStatus = "Sensor Active & Ready";

    public ConnectionSuccessViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
        Title = "Pairing Complete";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var device = await _deviceService.GetConfiguredDeviceAsync();
        if (device != null)
        {
            DeviceIdentifier = device.MaskedSerialNumber;
            FirmwareVersion = device.FirmwareVersion;
            BatteryStatus = $"{device.BatteryStatusText} ({device.BatteryVoltageMv} mV)";
            SensorStatus = device.TemperatureCelsius > 0
                ? $"Active • {device.TemperatureCelsius:F1} °C"
                : "Sensor Active & Ready";
        }
    }

    [RelayCommand]
    private async Task GoToDashboardAsync()
    {
        Preferences.Default.Set("cgm_device_configured", true);
        await Shell.Current.GoToAsync("//DashboardPage");
    }
}
