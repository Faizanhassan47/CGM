using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.ViewModels.Auth;

[QueryProperty(nameof(Email), "email")]
public partial class ResetPasswordViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _otpCode = string.Empty;

    [ObservableProperty]
    private string _newPassword = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private bool _isPasswordHidden = true;

    [ObservableProperty]
    private bool _isConfirmPasswordHidden = true;

    [ObservableProperty]
    private string _passwordIcon = FontAwesomeIcons.Eye;

    [ObservableProperty]
    private string _confirmPasswordIcon = FontAwesomeIcons.Eye;

    public ResetPasswordViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Set New Password";
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
        PasswordIcon = IsPasswordHidden ? FontAwesomeIcons.Eye : FontAwesomeIcons.EyeSlash;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordHidden = !IsConfirmPasswordHidden;
        ConfirmPasswordIcon = IsConfirmPasswordHidden ? FontAwesomeIcons.Eye : FontAwesomeIcons.EyeSlash;
    }

    [RelayCommand]
    private async Task ResetPasswordAsync()
    {
        HasError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(OtpCode))
        {
            ErrorMessage = "Please enter the 6-digit reset code sent to your email.";
            HasError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(NewPassword))
        {
            ErrorMessage = "Please enter a new password.";
            HasError = true;
            return;
        }

        if (NewPassword.Length < 8)
        {
            ErrorMessage = "Password must be at least 8 characters long.";
            HasError = true;
            return;
        }

        if (NewPassword != ConfirmPassword)
        {
            ErrorMessage = "Passwords do not match. Please re-enter.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            var (success, message) = await _authService.ResetPasswordAsync(Email, OtpCode, NewPassword);
            IsBusy = false;

            if (!success)
            {
                ErrorMessage = message;
                HasError = true;
                return;
            }

            await Shell.Current.DisplayAlert("Password Updated", "Your password has been reset successfully. Please log in with your new password.", "OK");
            await Shell.Current.GoToAsync("//LoginPage");
        }
        catch (Exception ex)
        {
            IsBusy = false;
            ErrorMessage = $"Password reset failed: {ex.Message}";
            HasError = true;
        }
    }

    [RelayCommand]
    private async Task ResendCodeAsync()
    {
        if (string.IsNullOrWhiteSpace(Email))
        {
            ErrorMessage = "Email address is missing. Please return to the previous screen.";
            HasError = true;
            return;
        }

        try
        {
            IsBusy = true;
            var (success, message) = await _authService.ForgotPasswordAsync(Email);
            IsBusy = false;

            if (success)
            {
                await Shell.Current.DisplayAlert("Code Sent", $"A new reset code has been sent to {Email}. It is valid for 15 minutes.", "OK");
            }
            else
            {
                ErrorMessage = message;
                HasError = true;
            }
        }
        catch (Exception ex)
        {
            IsBusy = false;
            ErrorMessage = ex.Message;
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
