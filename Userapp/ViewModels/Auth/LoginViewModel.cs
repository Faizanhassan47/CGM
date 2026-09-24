using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.Auth;

public partial class LoginViewModel : BaseViewModel
{
    private readonly IAuthService _authService;
    private readonly ISocialAuthService _socialAuthService;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isPasswordHidden = true;

    [ObservableProperty]
    private bool _rememberMe = true;

    [ObservableProperty]
    private bool _isAppleSignInAvailable;

    public LoginViewModel(IAuthService authService, ISocialAuthService socialAuthService)
    {
        _authService = authService;
        _socialAuthService = socialAuthService;
        Title = "Sign In";
        
        // Apple Sign-In is native to iOS
        IsAppleSignInAvailable = DeviceInfo.Platform == DevicePlatform.iOS;
    }

    [RelayCommand]
    private void TogglePasswordVisibility()
    {
        IsPasswordHidden = !IsPasswordHidden;
    }

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (IsBusy) return;

        ClearError();

        if (string.IsNullOrWhiteSpace(Email))
        {
            SetError("Please enter your email address.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            SetError("Please enter your password.");
            return;
        }

        try
        {
            IsBusy = true;
            var request = new LoginRequest
            {
                Email = Email.Trim(),
                Password = Password,
                RememberMe = RememberMe
            };

            var response = await _authService.LoginAsync(request);

            if (!response.Success || response.User == null)
            {
                SetError(response.Message ?? "Invalid email or password. Please try again.");
                return;
            }

            if (!response.User.IsProfileComplete)
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
            SetError("Unable to connect to service. Please check your network connection.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task GoogleSignInAsync()
    {
        if (IsBusy) return;
        ClearError();

        try
        {
            IsBusy = true;
            var idToken = await _socialAuthService.AuthenticateWithGoogleAsync();
            if (string.IsNullOrEmpty(idToken))
            {
                SetError("Google sign-in was canceled.");
                return;
            }

            var response = await _authService.LoginWithGoogleAsync(idToken);
            if (!response.Success || response.User == null)
            {
                SetError(response.Message ?? "Google sign-in failed.");
                return;
            }

            if (!response.User.IsProfileComplete)
            {
                await Shell.Current.GoToAsync("//CompleteProfilePage");
                return;
            }

            await Shell.Current.GoToAsync("//DashboardPage");
        }
        catch (Exception)
        {
            SetError("Google sign-in could not be completed. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task AppleSignInAsync()
    {
        if (IsBusy) return;
        ClearError();

        try
        {
            IsBusy = true;
            var appleRequest = await _socialAuthService.AuthenticateWithAppleAsync();
            if (appleRequest == null)
            {
                SetError("Apple sign-in was canceled.");
                return;
            }

            var response = await _authService.LoginWithAppleAsync(appleRequest);
            if (!response.Success || response.User == null)
            {
                SetError(response.Message ?? "Apple sign-in failed.");
                return;
            }

            if (!response.User.IsProfileComplete)
            {
                await Shell.Current.GoToAsync("//CompleteProfilePage");
                return;
            }

            await Shell.Current.GoToAsync("//DashboardPage");
        }
        catch (Exception)
        {
            SetError("Apple sign-in could not be completed. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task NavigateToSignUpAsync()
    {
        ClearError();
        await Shell.Current.GoToAsync("//SignUpPage");
    }

    [RelayCommand]
    private async Task ForgotPasswordAsync()
    {
        ClearError();
        await Shell.Current.GoToAsync("ForgotPasswordPage");
    }
}
