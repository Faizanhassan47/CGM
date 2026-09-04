using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Auth;

/// <summary>Production-safe placeholder until platform OAuth client IDs and redirect URIs are supplied.</summary>
public sealed class UnavailableSocialAuthService : ISocialAuthService
{
    public Task<string?> AuthenticateWithGoogleAsync() => Task.FromResult<string?>(null);
    public Task<AppleAuthRequest?> AuthenticateWithAppleAsync() => Task.FromResult<AppleAuthRequest?>(null);
}
