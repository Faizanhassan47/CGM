using CGM.PatientApp.ViewModels.Auth;

namespace CGM.PatientApp.Views.Auth;

public partial class VerifyEmailPage : ContentPage
{
    public VerifyEmailPage(VerifyEmailViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
