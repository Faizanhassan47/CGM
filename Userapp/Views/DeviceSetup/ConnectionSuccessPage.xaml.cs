using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class ConnectionSuccessPage : ContentPage
{
    private readonly ConnectionSuccessViewModel _viewModel;

    public ConnectionSuccessPage() : this(IPlatformApplication.Current?.Services?.GetService<ConnectionSuccessViewModel>() ?? throw new InvalidOperationException("ConnectionSuccessViewModel not found"))
    {
    }

    public ConnectionSuccessPage(ConnectionSuccessViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }
}
