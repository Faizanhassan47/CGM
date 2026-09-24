using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class ConnectingPage : ContentPage
{
    private readonly ConnectingViewModel _viewModel;

    public ConnectingPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<ConnectingViewModel>() 
        ?? throw new InvalidOperationException("ConnectingViewModel not found"))
    {
    }

    public ConnectingPage(ConnectingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.StartConnectionSequenceAsync, "ConnectingPage.OnAppearing");
    }
}
