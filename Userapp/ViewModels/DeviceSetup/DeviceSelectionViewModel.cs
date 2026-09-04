using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class DeviceSelectionViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;
    private readonly IBleService _bleService;

    [ObservableProperty]
    private ObservableCollection<SupportedCgmDevice> _devices = new();

    [ObservableProperty]
    private SupportedCgmDevice? _selectedDevice;

    [ObservableProperty]
    private bool _isDisposableSelected = true;

    [ObservableProperty]
    private bool _isReusableSelected = false;

    public DeviceSelectionViewModel(IDeviceService deviceService, IBleService? bleService = null)
    {
        _deviceService = deviceService;
        _bleService = bleService ?? IPlatformApplication.Current?.Services?.GetService<IBleService>()
            ?? throw new InvalidOperationException("Bluetooth service is not configured.");
        Title = "Choose Your CGM";
    }

    partial void OnSelectedDeviceChanged(SupportedCgmDevice? value)
    {
        if (value != null)
        {
            IsDisposableSelected = value.DeviceType == CgmDeviceType.Disposable;
            IsReusableSelected = value.DeviceType == CgmDeviceType.Reusable;
        }
    }

    [RelayCommand]
    public async Task LoadDevicesAsync()
    {
        try
        {
            IsBusy = true;
            var list = await _deviceService.GetSupportedDevicesAsync();
            Devices.Clear();
            foreach (var dev in list)
            {
                Devices.Add(dev);
            }

            if (SelectedDevice == null)
            {
                SelectedDevice = IsDisposableSelected 
                    ? Devices.FirstOrDefault(d => d.DeviceType == CgmDeviceType.Disposable) 
                    : Devices.FirstOrDefault(d => d.DeviceType == CgmDeviceType.Reusable);

                if (SelectedDevice == null && Devices.Count > 0)
                {
                    SelectedDevice = Devices[0];
                }
            }
        }
        catch (Exception)
        {
            SetError("Failed to load supported CGM devices.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void SelectDisposable()
    {
        IsDisposableSelected = true;
        IsReusableSelected = false;
        var dev = Devices.FirstOrDefault(d => d.DeviceType == CgmDeviceType.Disposable);
        if (dev != null)
        {
            SelectedDevice = dev;
        }
    }

    [RelayCommand]
    private void SelectReusable()
    {
        IsDisposableSelected = false;
        IsReusableSelected = true;
        var dev = Devices.FirstOrDefault(d => d.DeviceType == CgmDeviceType.Reusable);
        if (dev != null)
        {
            SelectedDevice = dev;
        }
    }

    [RelayCommand]
    private async Task ContinueWithSelectedDeviceAsync()
    {
        if (IsBusy) return;

        try
        {
            IsBusy = true;

            // Prompt user for Bluetooth permission
            bool isAllowed = await Shell.Current.DisplayAlert(
                "Bluetooth Permission Required",
                "GlucoTrack requires Bluetooth access to discover, connect to, and receive continuous glucose readings from your CGM device.\n\nAllow GlucoTrack to use Bluetooth?",
                "Allow",
                "Don't Allow");

            if (!isAllowed)
            {
                // User declined - do not go to the next screen!
                await Shell.Current.DisplayAlert(
                    "Permission Denied",
                    "Bluetooth permission was declined. GlucoTrack cannot connect to your CGM device without Bluetooth. Please grant permission to proceed.",
                    "OK");
                return;
            }

            // Also request platform BLE permissions
            try
            {
                var blePermission = await _bleService.RequestPermissionsAsync();
                if (!blePermission)
                {
                    await Shell.Current.DisplayAlert(
                        "Permission Denied",
                        "Bluetooth permission is disabled in your device settings. Please enable Bluetooth to continue.",
                        "OK");
                    return;
                }
            }
            catch
            {
                // Fallback for mock/desktop environments
            }

            if (SelectedDevice == null)
            {
                SelectedDevice = Devices.FirstOrDefault(d => IsDisposableSelected ? d.DeviceType == CgmDeviceType.Disposable : d.DeviceType == CgmDeviceType.Reusable);
            }

            var modelCode = SelectedDevice?.ModelCode ?? (IsDisposableSelected ? "G-AA" : "G-PA0");
            var devId = SelectedDevice?.Id ?? (IsDisposableSelected ? "DEV-G-AA" : "DEV-G-PA");

            // Save selected model in preferences
            Preferences.Default.Set("cgm_selected_device_model", modelCode);
            Preferences.Default.Set("cgm_selected_device_id", devId);

            // Proceed to Device Preparation
            await Shell.Current.GoToAsync("DevicePreparationPage");
        }
        catch (Exception ex)
        {
            SetError("Error connecting to device: " + ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SkipAsync()
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
