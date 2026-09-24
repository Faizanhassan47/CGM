using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class DevicePreparationPage : ContentPage
{
    private readonly DevicePreparationViewModel _viewModel;

    public DevicePreparationPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<DevicePreparationViewModel>() 
        ?? throw new InvalidOperationException("DevicePreparationViewModel not found"))
    {
    }

    public DevicePreparationPage(DevicePreparationViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.InitializeAsync, "DevicePreparationPage.OnAppearing");
    }
}
