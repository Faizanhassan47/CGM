using CGM.PatientApp.ViewModels.Dashboard;

namespace CGM.PatientApp.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage() : this(IPlatformApplication.Current?.Services?.GetService<DashboardViewModel>() ?? throw new InvalidOperationException("DashboardViewModel not found"))
    {
    }

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
    }

    protected override void OnDisappearing()
    {
        _viewModel.StopBackgroundWork();
        base.OnDisappearing();
    }
}
