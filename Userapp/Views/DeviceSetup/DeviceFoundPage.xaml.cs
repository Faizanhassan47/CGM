using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class DeviceFoundPage : ContentPage
{
    private readonly DeviceFoundViewModel _viewModel;

    public DeviceFoundPage() : this(IPlatformApplication.Current?.Services?.GetService<DeviceFoundViewModel>() ?? throw new InvalidOperationException("DeviceFoundViewModel not found"))
    {
    }

    public DeviceFoundPage(DeviceFoundViewModel viewModel)
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
