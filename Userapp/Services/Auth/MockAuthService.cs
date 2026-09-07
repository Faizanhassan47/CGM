using System.Net.Http.Json;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Auth;

public class MockAuthService : IAuthService, ISocialAuthService, IProfileService
{
    private const string AccessTokenKey = "cgm_access_token";
    private const string RefreshTokenKey = "cgm_refresh_token";
    private const string ProfileCompleteKey = "cgm_profile_complete";
    private const string DeviceConfiguredKey = "cgm_device_configured";
    private const string UserEmailKey = "cgm_user_email";
    private const string UserNameKey = "cgm_user_name";

    private AuthenticatedUser? _currentUser;
    private PatientProfile? _currentProfile;

    public async Task<bool> IsAuthenticatedAsync()
    {
        try
        {
            var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
            return !string.IsNullOrWhiteSpace(token);
        }
        catch
        {
            return false;
        }
    }

    public async Task<AuthenticatedUser?> GetCurrentUserAsync()
    {
        var email = Preferences.Default.Get(UserEmailKey, "alex.johnson@email.com");
        var name = Preferences.Default.Get(UserNameKey, "Alex Johnson");

        if (_currentUser != null)
        {
            _currentUser.Email = email;
            _currentUser.FullName = name;
            return _currentUser;
        }

        var token = await SecureStorage.Default.GetAsync(AccessTokenKey);
        if (string.IsNullOrEmpty(token))
            return null;

        var isProfileComplete = Preferences.Default.Get(ProfileCompleteKey, false);

        _currentUser = new AuthenticatedUser
        {
            UserId = "USR-1001",
            Email = email,
            FullName = name,
            AccessToken = token,
            RefreshToken = "mock-refresh-token",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsEmailVerified = true,
            IsProfileComplete = isProfileComplete
        };

        return _currentUser;
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        // Simulate network delay
        await Task.Delay(600);

        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Please enter both email and password."
            };
        }

        // Demo credential validation (accept demo/valid logins)
        if (request.Password.Length < 6)
        {
            return new LoginResponse
            {
                Success = false,
                Message = "Password must be at least 6 characters."
            };
        }

        var isProfileComplete = Preferences.Default.Get(ProfileCompleteKey, true);
        var name = Preferences.Default.Get(UserNameKey, "Alex Johnson");

        _currentUser = new AuthenticatedUser
        {
            UserId = "USR-1001",
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = name,
            AccessToken = "mock-jwt-access-token-" + Guid.NewGuid().ToString("N"),
            RefreshToken = "mock-jwt-refresh-token-" + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsEmailVerified = true,
            IsProfileComplete = isProfileComplete
        };

        await SecureStorage.Default.SetAsync(AccessTokenKey, _currentUser.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, _currentUser.RefreshToken);
        Preferences.Default.Set(UserEmailKey, _currentUser.Email);
        Preferences.Default.Set(UserNameKey, _currentUser.FullName);

        return new LoginResponse
        {
            Success = true,
            Message = "Login successful.",
            User = _currentUser
        };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        await Task.Delay(600);

        if (string.IsNullOrWhiteSpace(request.FullName))
            return new RegisterResponse { Success = false, Message = "Full Name is required." };

        if (string.IsNullOrWhiteSpace(request.Email) || !request.Email.Contains('@'))
            return new RegisterResponse { Success = false, Message = "A valid email address is required." };

        if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
            return new RegisterResponse { Success = false, Message = "Password must be at least 6 characters." };

        if (request.Password != request.ConfirmPassword)
            return new RegisterResponse { Success = false, Message = "Passwords do not match." };

        _currentUser = new AuthenticatedUser
        {
            UserId = "USR-1001",
            Email = request.Email.Trim().ToLowerInvariant(),
            FullName = request.FullName.Trim(),
            AccessToken = "mock-jwt-access-token-" + Guid.NewGuid().ToString("N"),
            RefreshToken = "mock-jwt-refresh-token-" + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsEmailVerified = true,
            IsProfileComplete = false
        };

        await SecureStorage.Default.SetAsync(AccessTokenKey, _currentUser.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, _currentUser.RefreshToken);
        Preferences.Default.Set(UserEmailKey, _currentUser.Email);
        Preferences.Default.Set(UserNameKey, _currentUser.FullName);
        Preferences.Default.Set(ProfileCompleteKey, false);

        RegisterNewUserInRegistry(request.Email, request.FullName);

        return new RegisterResponse
        {
            Success = true,
            Message = "Account created successfully.",
            Email = request.Email.Trim().ToLowerInvariant(),
            RequiresVerification = false
        };
    }

    public async Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailRequest request)
    {
        await Task.Delay(500);

        if (string.IsNullOrWhiteSpace(request.VerificationCode) || request.VerificationCode.Trim().Length != 6)
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Please enter a valid 6-digit verification code.",
                Data = false
            };
        }

        // Accept any 6 digit code for mock demonstration (e.g. 123456)
        var email = !string.IsNullOrWhiteSpace(request.Email) 
            ? request.Email 
            : Preferences.Default.Get(UserEmailKey, "alex.johnson@email.com");
        var name = Preferences.Default.Get(UserNameKey, "Alex Johnson");

        _currentUser = new AuthenticatedUser
        {
            UserId = "USR-1001",
            Email = email,
            FullName = name,
            AccessToken = "mock-jwt-access-token-" + Guid.NewGuid().ToString("N"),
            RefreshToken = "mock-jwt-refresh-token-" + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsEmailVerified = true,
            IsProfileComplete = false
        };

        await SecureStorage.Default.SetAsync(AccessTokenKey, _currentUser.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, _currentUser.RefreshToken);

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Email verified successfully.",
            Data = true
        };
    }

    public async Task<ApiResponse<bool>> ResendVerificationCodeAsync(ResendVerificationRequest request)
    {
        await Task.Delay(400);
        return new ApiResponse<bool>
        {
            Success = true,
            Message = "A new 6-digit verification code has been sent to your email.",
            Data = true
        };
    }

    public async Task<LoginResponse> LoginWithGoogleAsync(string idToken)
    {
        await Task.Delay(600);

        _currentUser = new AuthenticatedUser
        {
            UserId = "GOOGLE-1001",
            Email = "google.patient@example.com",
            FullName = "Google User",
            AccessToken = "mock-google-jwt-" + Guid.NewGuid().ToString("N"),
            RefreshToken = "mock-google-refresh-" + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsEmailVerified = true,
            IsProfileComplete = Preferences.Default.Get(ProfileCompleteKey, false)
        };

        await SecureStorage.Default.SetAsync(AccessTokenKey, _currentUser.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, _currentUser.RefreshToken);
        Preferences.Default.Set(UserEmailKey, _currentUser.Email);
        Preferences.Default.Set(UserNameKey, _currentUser.FullName);

        return new LoginResponse
        {
            Success = true,
            Message = "Signed in with Google.",
            User = _currentUser
        };
    }

    public async Task<LoginResponse> LoginWithAppleAsync(AppleAuthRequest request)
    {
        await Task.Delay(600);

        _currentUser = new AuthenticatedUser
        {
            UserId = "APPLE-1001",
            Email = request.Email ?? "apple.patient@privaterelay.appleid.com",
            FullName = request.FullName ?? "Apple User",
            AccessToken = "mock-apple-jwt-" + Guid.NewGuid().ToString("N"),
            RefreshToken = "mock-apple-refresh-" + Guid.NewGuid().ToString("N"),
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsEmailVerified = true,
            IsProfileComplete = Preferences.Default.Get(ProfileCompleteKey, false)
        };

        await SecureStorage.Default.SetAsync(AccessTokenKey, _currentUser.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, _currentUser.RefreshToken);
        Preferences.Default.Set(UserEmailKey, _currentUser.Email);
        Preferences.Default.Set(UserNameKey, _currentUser.FullName);

        return new LoginResponse
        {
            Success = true,
            Message = "Signed in with Apple.",
            User = _currentUser
        };
    }

    public async Task<ApiResponse<bool>> RefreshTokenAsync()
    {
        var refresh = await SecureStorage.Default.GetAsync(RefreshTokenKey);
        if (string.IsNullOrEmpty(refresh))
            return new ApiResponse<bool> { Success = false, Message = "No refresh token available." };

        var newAccess = "mock-refreshed-jwt-" + Guid.NewGuid().ToString("N");
        await SecureStorage.Default.SetAsync(AccessTokenKey, newAccess);

        return new ApiResponse<bool> { Success = true, Message = "Token refreshed.", Data = true };
    }

    public async Task LogoutAsync()
    {
        _currentUser = null;
        _currentProfile = null;
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        await Task.CompletedTask;
    }

    public async Task<ApiResponse<bool>> DeleteAccountAsync()
    {
        await LogoutAsync();
        Preferences.Default.Clear();
        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Account deleted successfully.",
            Data = true
        };
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        await Task.Delay(250);
        if (string.IsNullOrWhiteSpace(currentPassword)) return (false, "Enter your current password.");
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6) return (false, "New password must be at least 6 characters.");
        await LogoutAsync();
        return (true, "Password changed successfully. Please sign in again.");
    }

    public Task<bool> HasCompletedProfileAsync()
    {
        var completed = Preferences.Default.Get(ProfileCompleteKey, false);
        return Task.FromResult(completed);
    }

    public Task<bool> HasConfiguredDeviceAsync()
    {
        var configured = Preferences.Default.Get(DeviceConfiguredKey, false);
        return Task.FromResult(configured);
    }

    // ISocialAuthService Implementation
    public async Task<string?> AuthenticateWithGoogleAsync()
    {
        await Task.Delay(400);
        return "mock_google_id_token_" + Guid.NewGuid().ToString("N");
    }

    public async Task<AppleAuthRequest?> AuthenticateWithAppleAsync()
    {
        await Task.Delay(400);
        return new AppleAuthRequest
        {
            IdentityToken = "mock_apple_identity_token_" + Guid.NewGuid().ToString("N"),
            AuthorizationCode = "mock_auth_code",
            FullName = "Apple User",
            Email = "apple.patient@privaterelay.appleid.com"
        };
    }

    // IProfileService Implementation
    public Task<PatientProfile?> GetProfileAsync()
    {
        var name = Preferences.Default.Get(UserNameKey, "Alex Johnson");
        var email = Preferences.Default.Get(UserEmailKey, "alex.johnson@email.com");
        var unitStr = Preferences.Default.Get("glucose_unit", (string?)null);
        var unitInt = unitStr != null
            ? (unitStr.Equals("mmol/L", StringComparison.OrdinalIgnoreCase) ? (int)GlucoseUnit.MmolL : (int)GlucoseUnit.MgDl)
            : Preferences.Default.Get(nameof(GlucoseUnit), (int)GlucoseUnit.MgDl);

        if (_currentProfile != null)
        {
            _currentProfile.FullName = name;
            _currentProfile.Email = email;
            _currentProfile.PreferredGlucoseUnit = (GlucoseUnit)unitInt;
            return Task.FromResult<PatientProfile?>(_currentProfile);
        }

        _currentProfile = new PatientProfile
        {
            UserId = "USR-1001",
            FullName = name,
            Email = email,
            PreferredGlucoseUnit = (GlucoseUnit)unitInt,
            EnablePushNotifications = true,
            EnableCriticalAlerts = true,
            ThemePreference = Preferences.Default.Get("ThemePreference", "System")
        };

        return Task.FromResult<PatientProfile?>(_currentProfile);
    }

    public Task<ApiResponse<PatientProfile>> SaveProfileAsync(PatientProfile profile)
    {
        _currentProfile = profile;
        Preferences.Default.Set(UserNameKey, profile.FullName);
        Preferences.Default.Set(UserEmailKey, profile.Email);
        Preferences.Default.Set(nameof(GlucoseUnit), (int)profile.PreferredGlucoseUnit);
        Preferences.Default.Set("glucose_unit", profile.PreferredGlucoseUnit == GlucoseUnit.MmolL ? "mmol/L" : "mg/dL");
        Preferences.Default.Set("ThemePreference", profile.ThemePreference);

        if (_currentUser != null)
        {
            _currentUser.FullName = profile.FullName;
            _currentUser.Email = profile.Email;
        }

        return Task.FromResult(new ApiResponse<PatientProfile>
        {
            Success = true,
            Message = "Profile updated.",
            Data = profile
        });
    }

    public async Task<ApiResponse<bool>> CompleteProfileAsync(CompleteProfileRequest request)
    {
        await Task.Delay(500);

        if (string.IsNullOrWhiteSpace(request.FullName))
        {
            return new ApiResponse<bool>
            {
                Success = false,
                Message = "Full name is required."
            };
        }

        Preferences.Default.Set(UserNameKey, request.FullName.Trim());
        Preferences.Default.Set(nameof(GlucoseUnit), (int)request.PreferredGlucoseUnit);
        Preferences.Default.Set("glucose_unit", request.PreferredGlucoseUnit == GlucoseUnit.MmolL ? "mmol/L" : "mg/dL");
        Preferences.Default.Set(ProfileCompleteKey, true);

        if (_currentUser != null)
        {
            _currentUser.FullName = request.FullName.Trim();
            _currentUser.IsProfileComplete = true;
        }

        if (_currentProfile != null)
        {
            _currentProfile.FullName = request.FullName.Trim();
            _currentProfile.PreferredGlucoseUnit = request.PreferredGlucoseUnit;
        }

        return new ApiResponse<bool>
        {
            Success = true,
            Message = "Profile completed successfully.",
            Data = true
        };
    }

    private const string RegisteredUsersKey = "cgm_registered_users_registry";

    private static Dictionary<string, string> GetRegisteredUsers()
    {
        var baseUsers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "faizanhassan47@gmail.com", "Faizan Hassan" },
            { "cgm@gms-world.co", "GlucoTrack Admin" },
            { "jane.doe@email.com", "Jane Doe" },
            { "alex.johnson@email.com", "Alex Johnson" }
        };

        var storedJson = Preferences.Default.Get(RegisteredUsersKey, string.Empty);
        if (!string.IsNullOrEmpty(storedJson))
        {
            try
            {
                var stored = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(storedJson);
                if (stored != null)
                {
                    foreach (var kvp in stored)
                    {
                        baseUsers[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch { }
        }

        var currentStoredEmail = Preferences.Default.Get(UserEmailKey, string.Empty)?.Trim().ToLowerInvariant();
        var currentStoredName = Preferences.Default.Get(UserNameKey, string.Empty);
        if (!string.IsNullOrEmpty(currentStoredEmail))
        {
            baseUsers[currentStoredEmail] = !string.IsNullOrEmpty(currentStoredName) ? currentStoredName : "User";
        }

        return baseUsers;
    }

    private static void RegisterNewUserInRegistry(string email, string name)
    {
        var users = GetRegisteredUsers();
        users[email.Trim().ToLowerInvariant()] = name.Trim();
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(users);
            Preferences.Default.Set(RegisteredUsersKey, json);
        }
        catch { }
    }

    private static readonly Dictionary<string, (string OtpCode, string Token, DateTime ExpiresAt)> _activeResetCodes = new(StringComparer.OrdinalIgnoreCase);

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email)
    {
        await Task.Delay(600);
        var normalizedEmail = email?.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return (false, "Please enter your email address.");
        }

        if (!normalizedEmail.Contains("@") || !normalizedEmail.Contains("."))
        {
            return (false, "Please enter a valid email address.");
        }

        // 1. Try validating against backend SQL Database API if reachable
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            var response = await client.PostAsJsonAsync("http://10.0.2.2:5000/api/auth/forgot-password", new { email = normalizedEmail });
            if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return (false, "No account found with this email address. Please check your email or sign up.");
            }
        }
        catch { /* Fallback to local user registry check */ }

        // 2. Strict database/account existence check
        var registeredUsers = GetRegisteredUsers();
        if (!registeredUsers.ContainsKey(normalizedEmail))
        {
            return (false, "No account found with this email address. Please check your email or sign up.");
        }

        // Explicit demo-only reset flow. Production uses ApiAuthService and email delivery on the server.
        const string otp = "123456";
        var token = Guid.NewGuid().ToString("N");
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        _activeResetCodes[normalizedEmail] = (otp, token, expiresAt);

        return (true, "Demo reset code: 123456 (valid for 15 minutes). No email was sent.");
    }

    public async Task<(bool Success, string Message)> VerifyResetCodeAsync(string email, string code)
    {
        await Task.Delay(400);
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedCode = code?.Trim() ?? string.Empty;

        if (!_activeResetCodes.TryGetValue(normalizedEmail, out var session))
        {
            // Allow master debug code 123456 or 786012 for tests
            if (normalizedCode == "123456" || normalizedCode == "786012")
            {
                return (true, "Reset code verified.");
            }
            return (false, "No active reset request found. Please request a new reset code.");
        }

        if (session.ExpiresAt < DateTime.UtcNow)
        {
            _activeResetCodes.Remove(normalizedEmail);
            return (false, "This reset code has expired (15-minute limit). Please request a new code.");
        }

        if (session.OtpCode != normalizedCode && normalizedCode != "123456")
        {
            return (false, "Invalid reset code. Please check your email and try again.");
        }

        return (true, "Reset code verified successfully.");
    }

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string tokenOrCode, string newPassword)
    {
        await Task.Delay(600);
        var normalizedEmail = email?.Trim().ToLowerInvariant() ?? string.Empty;
        var normalizedCode = tokenOrCode?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 6)
        {
            return (false, "Password must be at least 6 characters long.");
        }

        if (_activeResetCodes.TryGetValue(normalizedEmail, out var session))
        {
            if (session.ExpiresAt < DateTime.UtcNow)
            {
                _activeResetCodes.Remove(normalizedEmail);
                return (false, "Your reset session has expired (15-minute limit). Please request a new code.");
            }

            _activeResetCodes.Remove(normalizedEmail);
        }

        // Save updated password state
        Preferences.Default.Set("cgm_user_password_updated", DateTime.UtcNow.ToString("O"));

        // Also try calling the live API endpoint
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
            await client.PostAsJsonAsync("http://10.0.2.2:5000/api/auth/reset-password", new
            {
                email = normalizedEmail,
                tokenOrCode = normalizedCode,
                newPassword = newPassword
            });
        }
        catch { /* Fallback */ }

        return (true, "Your password has been successfully updated. You can now sign in with your new password.");
    }
}
