using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class LoginRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public bool RememberMe { get; set; } = true;
}

public class RegisterRequest
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string ConfirmPassword { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public bool AgreeToTerms { get; set; }
}

public class VerifyEmailRequest
{
    public string Email { get; set; } = string.Empty;
    public string VerificationCode { get; set; } = string.Empty;
}

public class ResendVerificationRequest
{
    public string Email { get; set; } = string.Empty;
}

public class GoogleAuthRequest
{
    public string IdToken { get; set; } = string.Empty;
}

public class AppleAuthRequest
{
    public string IdentityToken { get; set; } = string.Empty;
    public string? AuthorizationCode { get; set; }
    public string? FullName { get; set; }
    public string? Email { get; set; }
}

public class CompleteProfileRequest
{
    public string FullName { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public GlucoseUnit PreferredGlucoseUnit { get; set; } = GlucoseUnit.MgDl;
    public bool EnablePushNotifications { get; set; } = true;
    public bool AcknowledgedTerms { get; set; } = true;
}
