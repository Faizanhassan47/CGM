using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class ScanningPage : ContentPage
{
    private readonly ScanningViewModel _viewModel;

    public ScanningPage() : this(IPlatformApplication.Current?.Services?.GetService<ScanningViewModel>() ?? throw new InvalidOperationException("ScanningViewModel not found"))
    {
    }

    public ScanningPage(ScanningViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.StartScanAsync();
    }

    protected override async void OnDisappearing()
    {
        base.OnDisappearing();
        await _viewModel.StopScanAsync();
    }
}
