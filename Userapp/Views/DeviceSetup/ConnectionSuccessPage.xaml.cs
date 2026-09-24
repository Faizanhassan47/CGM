using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class ConnectionSuccessPage : ContentPage
{
    private readonly ConnectionSuccessViewModel _viewModel;

    public ConnectionSuccessPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<ConnectionSuccessViewModel>() 
        ?? throw new InvalidOperationException("ConnectionSuccessViewModel not found"))
    {
    }

    public ConnectionSuccessPage(ConnectionSuccessViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.InitializeAsync, "ConnectionSuccessPage.OnAppearing");
    }
}
