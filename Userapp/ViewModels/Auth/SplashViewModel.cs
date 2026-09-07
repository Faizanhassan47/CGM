using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using Microsoft.Maui.ApplicationModel;

namespace CGM.PatientApp.ViewModels.Auth;

public partial class SplashViewModel : BaseViewModel
{
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _statusMessage = "Initializing secure environment...";

    public SplashViewModel(IAuthService authService)
    {
        _authService = authService;
        Title = "Splash";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        try
        {
            IsBusy = true;
            StatusMessage = "Checking session status...";
            await Task.Delay(400); // Smooth branded transition

            string targetRoute = "//LoginPage";
            bool isAuthenticated = false;

            try
            {
                isAuthenticated = await _authService.IsAuthenticatedAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SplashViewModel] Auth check error: {ex.Message}");
            }

            if (isAuthenticated)
            {
                StatusMessage = "Loading patient profile...";
                bool hasProfile = await _authService.HasCompletedProfileAsync();
                if (!hasProfile)
                {
                    targetRoute = "//CompleteProfilePage";
                }
                else
                {
                    StatusMessage = "Checking device connection...";
                    bool hasDevice = await _authService.HasConfiguredDeviceAsync();
                    targetRoute = hasDevice ? "//DashboardPage" : "//DeviceSelectionPage";
                }
            }

            await NavigateToTargetAsync(targetRoute);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SplashViewModel] Navigation error: {ex.Message}");
            await NavigateToTargetAsync("//LoginPage");
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task NavigateToTargetAsync(string route)
    {
        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            if (Shell.Current != null)
            {
                await Shell.Current.GoToAsync(route);
            }
        });
    }
}
