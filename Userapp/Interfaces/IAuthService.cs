using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IAuthService
{
    Task<bool> IsAuthenticatedAsync();
    Task<AuthenticatedUser?> GetCurrentUserAsync();
    Task<LoginResponse> LoginAsync(LoginRequest request);
    Task<RegisterResponse> RegisterAsync(RegisterRequest request);
    Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailRequest request);
    Task<ApiResponse<bool>> ResendVerificationCodeAsync(ResendVerificationRequest request);
    Task<LoginResponse> LoginWithGoogleAsync(string idToken);
    Task<LoginResponse> LoginWithAppleAsync(AppleAuthRequest request);
    Task<ApiResponse<bool>> RefreshTokenAsync();
    Task LogoutAsync();
    Task<ApiResponse<bool>> DeleteAccountAsync();
    Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword);
    Task<bool> HasCompletedProfileAsync();
    Task<bool> HasConfiguredDeviceAsync();
    Task<(bool Success, string Message)> ForgotPasswordAsync(string email);
    Task<(bool Success, string Message)> VerifyResetCodeAsync(string email, string code);
    Task<(bool Success, string Message)> ResetPasswordAsync(string email, string tokenOrCode, string newPassword);
}
