using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.Auth;

public partial class CompleteProfileViewModel : BaseViewModel
{
    private readonly IProfileService _profileService;
    private readonly IAuthService _authService;

    [ObservableProperty]
    private string _fullName = string.Empty;

    [ObservableProperty]
    private string _phoneNumber = string.Empty;

    [ObservableProperty]
    private GlucoseUnit _selectedGlucoseUnit = GlucoseUnit.MgDl;

    [ObservableProperty]
    private bool _isMgDlSelected = true;

    [ObservableProperty]
    private bool _isMmolLSelected = false;

    [ObservableProperty]
    private bool _enableNotifications = true;

    [ObservableProperty]
    private bool _enableCriticalAlerts = true;

    public CompleteProfileViewModel(IProfileService profileService, IAuthService authService)
    {
        _profileService = profileService;
        _authService = authService;
        Title = "Complete Profile";
    }

    [RelayCommand]
    public async Task InitializeAsync()
    {
        var user = await _authService.GetCurrentUserAsync();
        if (user != null && !string.IsNullOrWhiteSpace(user.FullName))
        {
            FullName = user.FullName;
        }

        var savedUnitStr = Preferences.Default.Get("glucose_unit", (string?)null);
        if (savedUnitStr != null)
        {
            if (savedUnitStr.Equals("mmol/L", StringComparison.OrdinalIgnoreCase))
                SelectMmolL();
            else
                SelectMgDl();
        }
        else
        {
            var unitInt = Preferences.Default.Get(nameof(GlucoseUnit), (int)GlucoseUnit.MgDl);
            if (unitInt == (int)GlucoseUnit.MmolL)
                SelectMmolL();
            else
                SelectMgDl();
        }
    }

    [RelayCommand]
    private void SelectMgDl()
    {
        SelectedGlucoseUnit = GlucoseUnit.MgDl;
        IsMgDlSelected = true;
        IsMmolLSelected = false;
    }

    [RelayCommand]
    private void SelectMmolL()
    {
        SelectedGlucoseUnit = GlucoseUnit.MmolL;
        IsMmolLSelected = true;
        IsMgDlSelected = false;
    }

    [RelayCommand]
    private void SelectUnit(string? unit)
    {
        if (!string.IsNullOrWhiteSpace(unit) && unit.StartsWith("mmol", StringComparison.OrdinalIgnoreCase))
        {
            SelectMmolL();
        }
        else
        {
            SelectMgDl();
        }
    }

    [RelayCommand]
    private async Task SaveAndContinueAsync()
    {
        if (IsBusy) return;
        ClearError();

        if (string.IsNullOrWhiteSpace(FullName))
        {
            SetError("Please enter your full name.");
            return;
        }

        try
        {
            IsBusy = true;
            Preferences.Default.Set(nameof(GlucoseUnit), (int)SelectedGlucoseUnit);
            Preferences.Default.Set("glucose_unit", SelectedGlucoseUnit == GlucoseUnit.MmolL ? "mmol/L" : "mg/dL");

            var request = new CompleteProfileRequest
            {
                FullName = FullName.Trim(),
                PhoneNumber = string.IsNullOrWhiteSpace(PhoneNumber) ? null : PhoneNumber.Trim(),
                PreferredGlucoseUnit = SelectedGlucoseUnit,
                EnablePushNotifications = EnableNotifications,
                AcknowledgedTerms = true
            };

            var response = await _profileService.CompleteProfileAsync(request);

            if (!response.Success)
            {
                SetError(response.Message);
                return;
            }

            // Proceed to Device Selection
            await Shell.Current.GoToAsync("//DeviceSelectionPage");
        }
        catch (Exception)
        {
            SetError("Unable to save profile. Please try again.");
        }
        finally
        {
            IsBusy = false;
        }
    }
}
