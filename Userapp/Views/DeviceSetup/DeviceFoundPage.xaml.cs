using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class DeviceFoundPage : ContentPage
{
    private readonly DeviceFoundViewModel _viewModel;

    public DeviceFoundPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<DeviceFoundViewModel>() 
        ?? throw new InvalidOperationException("DeviceFoundViewModel not found"))
    {
    }

    public DeviceFoundPage(DeviceFoundViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.InitializeAsync, "DeviceFoundPage.OnAppearing");
    }
}
