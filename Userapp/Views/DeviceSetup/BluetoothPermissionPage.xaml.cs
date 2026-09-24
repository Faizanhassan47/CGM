using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class BluetoothPermissionPage : ContentPage
{
    public BluetoothPermissionPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<BluetoothPermissionViewModel>() 
        ?? throw new InvalidOperationException("BluetoothPermissionViewModel not found"))
    {
    }

    public BluetoothPermissionPage(BluetoothPermissionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
