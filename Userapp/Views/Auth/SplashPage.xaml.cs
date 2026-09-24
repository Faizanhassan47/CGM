using CGM.PatientApp.ViewModels.Auth;
using Microsoft.Extensions.DependencyInjection;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.Auth;

public partial class SplashPage : ContentPage
{
    private readonly SplashViewModel _viewModel;

    public SplashPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<SplashViewModel>()
        ?? new SplashViewModel(new Services.Auth.MockAuthService()))
    {
    }

    public SplashPage(SplashViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Dispatcher.Dispatch(() => SafeAsync.Run(_viewModel.InitializeAsync, "SplashPage.OnAppearing"));
    }
}
