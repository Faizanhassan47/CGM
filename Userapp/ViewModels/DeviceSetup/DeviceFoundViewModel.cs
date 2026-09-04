using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class DeviceFoundViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;

    [ObservableProperty]
    private string _deviceName = "CGM Sensor";

    [ObservableProperty]
    private string _deviceIdentifier = "Ending in C221";

    [ObservableProperty]
    private string _signalStrength = "Strong Signal";

    [ObservableProperty]
    private string _signalBadgeColor = "#10B981";

    public DeviceFoundViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
        Title = "CGM Found";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var rawName = Preferences.Default.Get("found_device_name", "CGM-C221");
        var rssi = Preferences.Default.Get("found_device_rssi", -127);

        DeviceName = "CGM Continuous Sensor";
        DeviceIdentifier = rawName.Length >= 4 ? $"Ending in {rawName[^4..]}" : "Potential CGM";
        SignalStrength = rssi >= -65 ? $"Strong Signal ({rssi} dBm)" : rssi >= -80 ? $"Good Signal ({rssi} dBm)" : $"Weak Signal ({rssi} dBm)";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        await Shell.Current.GoToAsync("ConnectingPage");
    }

    [RelayCommand]
    private async Task RescanAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
