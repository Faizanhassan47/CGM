using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.ViewModels.Auth;

public partial class ForgotPasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _identifier = string.Empty;

    public ForgotPasswordViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Forgot Password";
    }

    [RelayCommand]
    private async Task SendResetCodeAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        var email = Identifier?.Trim();
        if (string.IsNullOrWhiteSpace(email))
        {
            ErrorMessage = "Please enter your registered email address.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            var (success, message) = await _authService.ForgotPasswordAsync(email);
            IsBusy = false;

            if (!success)
            {
                ErrorMessage = message;
                HasError = true;
                return;
            }

            // Navigate to Reset Password Page with the email pre-populated
            await Shell.Current.GoToAsync($"//ResetPasswordPage?email={Uri.EscapeDataString(email)}");
        }
        catch (Exception ex)
        {
            IsBusy = false;
            ErrorMessage = $"Unable to send reset code: {ex.Message}";
            HasError = true;
        }
    }

    [RelayCommand]
    private async Task BackToLoginAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;
        await Shell.Current.GoToAsync("//LoginPage");
    }
}
