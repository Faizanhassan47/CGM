using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class ConnectingPage : ContentPage
{
    private readonly ConnectingViewModel _viewModel;

    public ConnectingPage() : this(IPlatformApplication.Current?.Services?.GetService<ConnectingViewModel>() ?? throw new InvalidOperationException("ConnectingViewModel not found"))
    {
    }

    public ConnectingPage(ConnectingViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.StartConnectionSequenceAsync();
    }
}
