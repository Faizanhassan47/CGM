using CGM.PatientApp.ViewModels.Auth;

namespace CGM.PatientApp.Views.Auth;

public partial class LoginPage : ContentPage
{
    public LoginPage() : this(IPlatformApplication.Current?.Services?.GetService<LoginViewModel>() ?? throw new InvalidOperationException("LoginViewModel not found"))
    {
    }

    public LoginPage(LoginViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
