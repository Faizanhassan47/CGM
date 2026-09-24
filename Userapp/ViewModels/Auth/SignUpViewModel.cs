using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.Auth;

public partial class SignUpViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ISocialAuthService _socialAuthService;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private string _confirmPassword = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private bool _agreeToTerms;

    [ObservableProperty]
    private bool _isPasswordHidden = true;

    [ObservableProperty]
    private bool _isConfirmPasswordHidden = true;

    [ObservableProperty]
    private bool _isAppleSignInAvailable;

    public SignUpViewModel(IAuthService authService, ISocialAuthService socialAuthService)
    {
        _authService = authService;
        _socialAuthService = socialAuthService;
        Title = "Create Account";
        IsAppleSignInAvailable = DeviceInfo.Platform == DevicePlatform.iOS;
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    [RelayCommand]
    private void ToggleConfirmPasswordVisibility()
    {
        IsConfirmPasswordHidden = !IsConfirmPasswordHidden;
    }

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (IsBusy) return;
        ClearError();

        if (string.IsNullOrWhiteSpace(FullName))
        {
            SetError("Please enter your full name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@') || !Email.Contains('.'))
        {
            SetError("Please enter a valid email address.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter a password.");
            return;
        }

        if (Password.Length < 8)
        {
            SetError("Password must be at least 8 characters.");
            return;
        }

        if (Password != ConfirmPassword)
        {
            SetError("Passwords do not match.");
            return;
        }

        if (!AgreeToTerms)
        {
            SetError("Please accept the Terms of Service and Privacy Policy to continue.");
            return;
        }

        try
        {
            IsBusy = true;
            var request = new RegisterRequest
            {
                FullName = FullName.Trim(),
                Email = Email.Trim(),
                Password = Password,
                ConfirmPassword = ConfirmPassword,
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                AgreeToTerms = AgreeToTerms
            };

            var response = await _authService.RegisterAsync(request);

            if (!response.Success)
            {
                SetError(response.Message);
                return;
            }

            bool hasCompletedProfile = await _authService.HasCompletedProfileAsync();
            if (!hasCompletedProfile)
            {
                await Shell.Current.GoToAsync("//CompleteProfilePage");
                return;
            }

            bool hasDevice = await _authService.HasConfiguredDeviceAsync();
            if (!hasDevice)
            {
                await Shell.Current.GoToAsync("//DeviceSelectionPage");
                return;
            }

            await Shell.Current.GoToAsync("//DashboardPage");
        }
        catch (Exception)
        {
            SetError("Unable to create account. Please check your network connection and try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoogleSignUpAsync()
    {
        if (IsBusy) return;
        ClearError();

        try
        {
            IsBusy = true;
            var idToken = await _socialAuthService.AuthenticateWithGoogleAsync();
            if (string.IsNullOrEmpty(idToken))
            {
                SetError("Google sign-up was canceled.");
                return;
            }

            var response = await _authService.LoginWithGoogleAsync(idToken);
            if (!response.Success || response.User == null)
            {
                SetError(response.Message ?? "Google registration failed.");
                return;
            }

            await Shell.Current.GoToAsync("//CompleteProfilePage");
        }
        catch (Exception)
        {
            SetError("Google registration failed. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AppleSignUpAsync()
    {
        if (IsBusy) return;
        ClearError();

        try
        {
            IsBusy = true;
            var appleRequest = await _socialAuthService.AuthenticateWithAppleAsync();
            if (appleRequest == null)
            {
                SetError("Apple sign-up was canceled.");
                return;
            }

            var response = await _authService.LoginWithAppleAsync(appleRequest);
            if (!response.Success || response.User == null)
            {
                SetError(response.Message ?? "Apple registration failed.");
                return;
            }

            await Shell.Current.GoToAsync("//CompleteProfilePage");
        }
        catch (Exception)
        {
            SetError("Apple registration failed. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToLoginAsync()
    {
        await Shell.Current.GoToAsync("..");
    }
}
