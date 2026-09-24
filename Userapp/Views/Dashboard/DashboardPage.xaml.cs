using CGM.PatientApp.ViewModels.Dashboard;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.Dashboard;

public partial class DashboardPage : ContentPage
{
    private readonly DashboardViewModel _viewModel;

    public DashboardPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<DashboardViewModel>() 
        ?? throw new InvalidOperationException("DashboardViewModel not found"))
    {
    }

    public DashboardPage(DashboardViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.Activate();
        SafeAsync.Run(_viewModel.InitializeAsync, "DashboardPage.OnAppearing");
    }

    protected override void OnDisappearing()
    {
        _viewModel.StopBackgroundWork();
        _viewModel.Deactivate();
        base.OnDisappearing();
    }
}
