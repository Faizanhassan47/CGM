using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class DeviceSelectionPage : ContentPage
{
    private readonly DeviceSelectionViewModel _viewModel;

    public DeviceSelectionPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<DeviceSelectionViewModel>() 
        ?? throw new InvalidOperationException("DeviceSelectionViewModel not found"))
    {
    }

    public DeviceSelectionPage(DeviceSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.LoadDevicesAsync, "DeviceSelectionPage.OnAppearing");
    }
}
