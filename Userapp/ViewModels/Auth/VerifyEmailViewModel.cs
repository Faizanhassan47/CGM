using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.ViewModels.Auth;

[QueryProperty(nameof(Email), "email")]
public partial class VerifyEmailViewModel(IAuthService authService) : BaseViewModel
{
    [ObservableProperty] private string _email = string.Empty;
    [ObservableProperty] private string _code = string.Empty;
    [ObservableProperty] private string _message = "Enter the six-digit code sent to your email.";

    [RelayCommand]
    private async Task VerifyAsync()
    {
        if (IsBusy || Code.Trim().Length != 6) { Message = "Enter a valid six-digit code."; return; }
        IsBusy = true;
        try
        {
            var result = await authService.VerifyEmailAsync(new VerifyEmailRequest { Email = Email, VerificationCode = Code.Trim() });
            Message = result.Message;
            if (result.Success) await Shell.Current.GoToAsync("//CompleteProfilePage");
        }
        finally { IsBusy = false; }
    }

    [RelayCommand]
    private async Task ResendAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        try { Message = (await authService.ResendVerificationCodeAsync(new ResendVerificationRequest { Email = Email })).Message; }
        finally { IsBusy = false; }
    }
}
