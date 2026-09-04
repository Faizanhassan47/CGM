using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;

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
            await Task.Delay(800); // Smooth branded transition

            bool isAuthenticated = await _authService.IsAuthenticatedAsync();

            if (isAuthenticated)
            {
                StatusMessage = "Loading patient profile...";
                bool hasProfile = await _authService.HasCompletedProfileAsync();
                if (!hasProfile)
                {
                    await Shell.Current.GoToAsync("//CompleteProfilePage");
                    return;
                }

                StatusMessage = "Checking device connection...";
                bool hasDevice = await _authService.HasConfiguredDeviceAsync();
                if (!hasDevice)
                {
                    await Shell.Current.GoToAsync("//DeviceSelectionPage");
                    return;
                }

                await Shell.Current.GoToAsync("//DashboardPage");
            }
            else
            {
                await Shell.Current.GoToAsync("//LoginPage");
            }
        }
        catch (Exception)
        {
            // Fail-safe to login on any unexpected error
            await Shell.Current.GoToAsync("//LoginPage");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
