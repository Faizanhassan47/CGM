using CGM.PatientApp.ViewModels.Auth;

namespace CGM.PatientApp.Views.Auth;

public partial class ResetPasswordPage : ContentPage
{
    public ResetPasswordPage(ResetPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public ResetPasswordPage() : this(
        IPlatformApplication.Current?.Services?.GetService<ResetPasswordViewModel>()
        ?? new ResetPasswordViewModel(IPlatformApplication.Current?.Services?.GetService<CGM.PatientApp.Interfaces.IAuthService>() ?? new CGM.PatientApp.Services.Auth.MockAuthService()))
    {
    }
}
