using CGM.PatientApp.ViewModels.DeviceSetup;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class DevicePreparationPage : ContentPage
{
    private readonly DevicePreparationViewModel _viewModel;

    public DevicePreparationPage() : this(IPlatformApplication.Current?.Services?.GetService<DevicePreparationViewModel>() ?? throw new InvalidOperationException("DevicePreparationViewModel not found"))
    {
    }

    public DevicePreparationPage(DevicePreparationViewModel viewModel)
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
