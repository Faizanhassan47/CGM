using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class BluetoothPermissionViewModel : BaseViewModel
{
    private readonly IBleService _bleService;

    [ObservableProperty]
    private string _permissionExplanation = "Bluetooth is required to establish a secure wireless connection with your Continuous Glucose Monitor and receive live glucose readings.";

    public BluetoothPermissionViewModel(IBleService bleService)
    {
        _bleService = bleService;
        Title = "Bluetooth Permission";
    }

    [RelayCommand]
    private async Task EnableBluetoothAsync()
    {
        try
        {
            IsBusy = true;
            // Check & request permissions
            var hasPermission = await _bleService.RequestPermissionsAsync();
            if (!hasPermission)
            {
                SetError("Bluetooth permission could not be granted. Please enable Bluetooth in your device settings.");
                return;
            }
            
            // Navigate to Scanning
            await Shell.Current.GoToAsync("ScanningPage");
        }
        catch (Exception)
        {
            SetError("Bluetooth permission could not be granted. Please enable Bluetooth in your device settings.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
