using CGM.PatientApp.Services.Auth;
using CGM.PatientApp.ViewModels.Auth;

namespace CGM.PatientApp.Views.Auth;

public partial class ForgotPasswordPage : ContentPage
{
    public ForgotPasswordPage(ForgotPasswordViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    public ForgotPasswordPage() : this(
        IPlatformApplication.Current?.Services?.GetService<ForgotPasswordViewModel>()
        ?? new ForgotPasswordViewModel(IPlatformApplication.Current?.Services?.GetService<CGM.PatientApp.Interfaces.IAuthService>() ?? new MockAuthService()))
    {
    }
}
