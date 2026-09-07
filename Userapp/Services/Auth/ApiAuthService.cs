using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Auth;

/// <summary>Production authentication client. Tokens never enter Preferences or application logs.</summary>
public sealed class ApiAuthService(HttpClient client) : IAuthService
{
    private const string AccessTokenKey = "cgm_access_token";
    private const string RefreshTokenKey = "cgm_refresh_token";
    private const string UserKey = "cgm_authenticated_user";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<bool> IsAuthenticatedAsync()
    {
        var user = await GetCurrentUserAsync();
        if (user is null) return false;
        if (user.ExpiresAt > DateTime.UtcNow.AddMinutes(1)) return true;
        return (await RefreshTokenAsync()).Success;
    }

    public async Task<AuthenticatedUser?> GetCurrentUserAsync()
    {
        try
        {
            var json = await SecureStorage.Default.GetAsync(UserKey);
            return string.IsNullOrWhiteSpace(json) ? null : JsonSerializer.Deserialize<AuthenticatedUser>(json, JsonOptions);
        }
        catch { return null; }
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
            return new() { Message = "Please enter both email and password." };

        var result = await SendAuthAsync("api/auth/login", new
        {
            Email = request.Email.Trim(), request.Password, request.RememberMe,
            DeviceInfo = DeviceInfo.Current.Platform.ToString()
        });
        return new() { Success = result.Success, Message = result.Message, User = result.User };
    }

    public async Task<RegisterResponse> RegisterAsync(RegisterRequest request)
    {
        if (request.Password != request.ConfirmPassword)
            return new() { Message = "Passwords do not match." };

        var result = await SendAuthAsync("api/auth/register", new
        {
            request.FullName, Email = request.Email.Trim(), request.Password, request.PhoneNumber,
            PreferredGlucoseUnit = "mg/dL"
        });
        return new()
        {
            Success = result.Success,
            Message = result.Message,
            Email = request.Email.Trim().ToLowerInvariant(),
            RequiresVerification = result.User is { IsEmailVerified: false }
        };
    }

    public Task<ApiResponse<bool>> VerifyEmailAsync(VerifyEmailRequest request) =>
        Task.FromResult(Unavailable("Email verification is not enabled by the API yet."));

    public Task<ApiResponse<bool>> ResendVerificationCodeAsync(ResendVerificationRequest request) =>
        Task.FromResult(Unavailable("Email verification is not enabled by the API yet."));

    public async Task<LoginResponse> LoginWithGoogleAsync(string idToken) =>
        await SocialLoginAsync(idToken, "Google", null);

    public async Task<LoginResponse> LoginWithAppleAsync(AppleAuthRequest request) =>
        await SocialLoginAsync(request.IdentityToken, "Apple", request.FullName);

    public async Task<ApiResponse<bool>> RefreshTokenAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (string.IsNullOrWhiteSpace(refreshToken)) return Unavailable("Your session has expired. Please sign in again.");
            var result = await SendAuthAsync("api/auth/refresh-token", new { RefreshToken = refreshToken });
            return new() { Success = result.Success, Message = result.Message, Data = result.Success };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Unavailable("Unable to refresh your session. Check your connection.");
        }
    }

    public async Task LogoutAsync()
    {
        try
        {
            var refreshToken = await SecureStorage.Default.GetAsync(RefreshTokenKey);
            if (!string.IsNullOrWhiteSpace(refreshToken))
                await client.PostAsJsonAsync("api/auth/logout", new { RefreshToken = refreshToken });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) { }
        finally { ClearSession(); }
    }

    public async Task<ApiResponse<bool>> DeleteAccountAsync()
    {
        try
        {
            if (!await IsAuthenticatedAsync())
                return Unavailable("Your session has expired. Please sign in again before deleting your account.");

            using var request = new HttpRequestMessage(HttpMethod.Delete, "api/auth/account");
            var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
            if (!string.IsNullOrWhiteSpace(accessToken))
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return Unavailable($"Account deletion could not be completed ({(int)response.StatusCode}).");
            ClearSession();
            Preferences.Default.Set("cgm_device_configured", false);
            return new() { Success = true, Message = "Account deleted.", Data = true };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return Unavailable("Account deletion service is unavailable.");
        }
    }

    public async Task<(bool Success, string Message)> ChangePasswordAsync(string currentPassword, string newPassword)
    {
        try
        {
            if (!await IsAuthenticatedAsync())
                return (false, "Your session has expired. Please sign in again.");

            using var request = new HttpRequestMessage(HttpMethod.Post, "api/auth/change-password");
            var accessToken = await SecureStorage.Default.GetAsync(AccessTokenKey);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            request.Content = JsonContent.Create(new { CurrentPassword = currentPassword, NewPassword = newPassword });
            using var response = await client.SendAsync(request);
            var body = await response.Content.ReadFromJsonAsync<ResetDto>(JsonOptions);
            if (!response.IsSuccessStatusCode || body?.Success != true)
                return (false, body?.Message ?? "Password could not be changed.");

            ClearSession();
            return (true, body.Message);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return (false, "Password service is unavailable. Check your connection.");
        }
    }

    public async Task<bool> HasCompletedProfileAsync() => (await GetCurrentUserAsync())?.IsProfileComplete ?? false;
    public Task<bool> HasConfiguredDeviceAsync() => Task.FromResult(Preferences.Default.Get("cgm_device_configured", false));

    public async Task<(bool Success, string Message)> ForgotPasswordAsync(string email) =>
        await SendResetAsync("api/auth/forgot-password", new { Email = email.Trim() });

    public async Task<(bool Success, string Message)> VerifyResetCodeAsync(string email, string code) =>
        await SendResetAsync("api/auth/verify-reset-code", new { Email = email.Trim(), Code = code.Trim() });

    public async Task<(bool Success, string Message)> ResetPasswordAsync(string email, string tokenOrCode, string newPassword) =>
        await SendResetAsync("api/auth/reset-password", new { Email = email.Trim(), TokenOrCode = tokenOrCode.Trim(), NewPassword = newPassword });

    private async Task<LoginResponse> SocialLoginAsync(string idToken, string provider, string? fullName)
    {
        var result = await SendAuthAsync("api/auth/social", new { IdToken = idToken, Provider = provider, FullName = fullName });
        return new() { Success = result.Success, Message = result.Message, User = result.User };
    }

    private async Task<(bool Success, string Message)> SendResetAsync(string path, object payload)
    {
        try
        {
            using var response = await client.PostAsJsonAsync(path, payload);
            var content = await response.Content.ReadAsStringAsync();
            ResetDto? body = null;
            if (!string.IsNullOrWhiteSpace(content))
            {
                try { body = JsonSerializer.Deserialize<ResetDto>(content, JsonOptions); }
                catch (JsonException) { }
            }
            return (response.IsSuccessStatusCode && body?.Success == true, body?.Message ?? FriendlyError(response.StatusCode));
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiAuth] Error during reset {path}: {ex}");
            return (false, "The service is unavailable. Check your connection and try again.");
        }
    }

    private async Task<(bool Success, string Message, AuthenticatedUser? User)> SendAuthAsync(string path, object payload)
    {
        try
        {
            using var response = await client.PostAsJsonAsync(path, payload);
            var content = await response.Content.ReadAsStringAsync();
            AuthDto? body = null;
            if (!string.IsNullOrWhiteSpace(content))
            {
                try { body = JsonSerializer.Deserialize<AuthDto>(content, JsonOptions); }
                catch (JsonException) { }
            }

            if (!response.IsSuccessStatusCode || body?.Success != true || body.User is null || string.IsNullOrWhiteSpace(body.AccessToken))
                return (false, body?.Message ?? FriendlyError(response.StatusCode), null);

            var user = new AuthenticatedUser
            {
                UserId = body.User.Id.ToString(), Email = body.User.Email, FullName = body.User.FullName,
                AccessToken = body.AccessToken, RefreshToken = body.RefreshToken ?? string.Empty,
                ExpiresAt = body.ExpiresAt ?? DateTime.UtcNow.AddMinutes(15),
                IsEmailVerified = body.User.EmailVerified, IsProfileComplete = body.User.ProfileCompleted
            };
            await StoreSessionAsync(user);
            return (true, body.Message, user);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiAuth] Error during {path}: {ex}");
            return (false, "The service is unavailable. Check your connection and try again.", null);
        }
    }

    private static async Task StoreSessionAsync(AuthenticatedUser user)
    {
        await SecureStorage.Default.SetAsync(AccessTokenKey, user.AccessToken);
        await SecureStorage.Default.SetAsync(RefreshTokenKey, user.RefreshToken);
        await SecureStorage.Default.SetAsync(UserKey, JsonSerializer.Serialize(user, JsonOptions));
        Preferences.Default.Set("cgm_user_name", user.FullName);
        Preferences.Default.Set("cgm_user_email", user.Email);
        Preferences.Default.Set("cgm_profile_complete", user.IsProfileComplete);
    }

    private static void ClearSession()
    {
        SecureStorage.Default.Remove(AccessTokenKey);
        SecureStorage.Default.Remove(RefreshTokenKey);
        SecureStorage.Default.Remove(UserKey);
    }

    private static ApiResponse<bool> Unavailable(string message) => new() { Success = false, Message = message, Data = false };
    private static string FriendlyError(HttpStatusCode code) => code == HttpStatusCode.Unauthorized
        ? "Invalid email or password." : "The request could not be completed. Please try again.";

    private sealed record AuthDto(bool Success, string Message, string? AccessToken, string? RefreshToken, DateTime? ExpiresAt, UserDto? User);
    private sealed record UserDto(int Id, string FullName, string Email, bool EmailVerified, bool ProfileCompleted);
    private sealed record ResetDto(bool Success, string Message);
}
