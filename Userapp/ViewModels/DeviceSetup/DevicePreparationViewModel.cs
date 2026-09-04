using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.DeviceSetup;

public partial class DevicePreparationViewModel : BaseViewModel
{
    private readonly IDeviceService _deviceService;

    [ObservableProperty]
    private string _deviceModelName = "Disposable CGM (G-AA)";

    [ObservableProperty]
    private string _preparationGuide = "Follow the official Instructions for Use supplied with your CGM packaging.";

    public DevicePreparationViewModel(IDeviceService deviceService)
    {
        _deviceService = deviceService;
        Title = "Prepare Your CGM";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var model = Preferences.Default.Get("cgm_selected_device_model", "G-AA");
        DeviceModelName = model.Contains("PA") ? "Reusable Transmitter (G-PA0 / G-PA1)" : "Disposable CGM (G-AA)";
        PreparationGuide = $"Follow the official Instructions for Use supplied with your {model} CGM.";
        await Task.CompletedTask;
    }

    [RelayCommand]
    private async Task ReadyAsync()
    {
        await Shell.Current.GoToAsync("BluetoothPermissionPage");
    }

    [RelayCommand]
    private async Task BackAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
