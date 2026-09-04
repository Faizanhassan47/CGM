using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class ScanningViewModel : BaseViewModel
{
    private readonly IBleService _bleService;
    private CancellationTokenSource? _scanCts;

    [ObservableProperty]
    private string _statusText = "Searching for your CGM...";

    [ObservableProperty]
    private string _helperText = "Keep your CGM close to your phone and ensure it is activated.";

    [ObservableProperty]
    private bool _isScanning = false;

    [ObservableProperty]
    private bool _hasTimedOut = false;

    public ScanningViewModel(IBleService bleService)
    {
        _bleService = bleService;
        Title = "Scanning";
    }

    [RelayCommand]
    public async Task StartScanAsync()
    {
        if (IsScanning) return;
        ClearError();
        HasTimedOut = false;
        IsScanning = true;
        StatusText = "Searching for nearby CGM...";

        _scanCts = new CancellationTokenSource();

        try
        {
            await _bleService.StartScanningAsync(async (discoveredDevice) =>
            {
                // Stop scanning and route to DeviceFoundPage with device info
                await StopScanAsync();
                await MainThread.InvokeOnMainThreadAsync(async () =>
                {
                    Preferences.Default.Set("found_device_name", discoveredDevice.DeviceName);
                    Preferences.Default.Set("found_device_id", discoveredDevice.BluetoothId);
                    Preferences.Default.Set("found_device_rssi", discoveredDevice.Rssi ?? -127);
                    await Shell.Current.GoToAsync("DeviceFoundPage");
                });
            }, _scanCts.Token);

            // Timeout after 15 seconds if not found
            await Task.Delay(15000, _scanCts.Token);
            if (IsScanning)
            {
                await StopScanAsync();
                HasTimedOut = true;
                StatusText = "No CGM device detected.";
                SetError("Could not find your CGM. Ensure it is close by and activated.");
            }
        }
        catch (TaskCanceledException)
        {
            // Scan was stopped intentionally
        }
        catch (Exception)
        {
            SetError("An error occurred during Bluetooth scanning.");
        }
        finally
        {
            IsScanning = false;
        }
    }

    [RelayCommand]
    public async Task StopScanAsync()
    {
        _scanCts?.Cancel();
        IsScanning = false;
        await _bleService.StopScanningAsync();
    }

    [RelayCommand]
    private async Task CancelAsync()
    {
        await StopScanAsync();
        await Shell.Current.GoToAsync("..");
    }
}
