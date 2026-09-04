using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class DeviceSelectionPage : ContentPage
{
    private readonly DeviceSelectionViewModel _viewModel;

    public DeviceSelectionPage() : this(IPlatformApplication.Current?.Services?.GetService<DeviceSelectionViewModel>() ?? throw new InvalidOperationException("DeviceSelectionViewModel not found"))
    {
    }

    public DeviceSelectionPage(DeviceSelectionViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadDevicesAsync();
    }
}
