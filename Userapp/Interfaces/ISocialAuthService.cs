using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface ISocialAuthService
{
    Task<string?> AuthenticateWithGoogleAsync();
    Task<AppleAuthRequest?> AuthenticateWithAppleAsync();
}
