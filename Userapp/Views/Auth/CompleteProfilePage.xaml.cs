using CGM.PatientApp.ViewModels.Auth;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.Auth;

public partial class CompleteProfilePage : ContentPage
{
    private readonly CompleteProfileViewModel _viewModel;

    public CompleteProfilePage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<CompleteProfileViewModel>() 
        ?? throw new InvalidOperationException("CompleteProfileViewModel not found"))
    {
    }

    public CompleteProfilePage(CompleteProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.InitializeAsync, "CompleteProfilePage.OnAppearing");
    }
}
