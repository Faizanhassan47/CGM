using CGM.PatientApp.ViewModels.Auth;

namespace CGM.PatientApp.Views.Auth;

public partial class SignUpPage : ContentPage
{
    public SignUpPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<SignUpViewModel>() 
        ?? throw new InvalidOperationException("SignUpViewModel not found"))
    {
    }

    public SignUpPage(SignUpViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
