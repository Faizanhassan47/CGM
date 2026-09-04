using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Auth;

public sealed class ApiProfileService(HttpClient client) : IProfileService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PatientProfile?> GetProfileAsync()
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, "api/profile");
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            var dto = await response.Content.ReadFromJsonAsync<ProfileDto>(JsonOptions);
            return dto is null ? null : Map(dto);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return null; }
    }

    public async Task<ApiResponse<PatientProfile>> SaveProfileAsync(PatientProfile profile)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Put, "api/profile");
            request.Content = JsonContent.Create(new
            {
                profile.FullName,
                profile.PhoneNumber,
                DateOfBirth = profile.DateOfBirth.HasValue ? DateOnly.FromDateTime(profile.DateOfBirth.Value) : (DateOnly?)null,
                Gender = (string?)null,
                PreferredGlucoseUnit = profile.PreferredGlucoseUnit == GlucoseUnit.MmolL ? "mmol/L" : "mg/dL",
                Theme = profile.ThemePreference,
                Language = profile.PreferredLanguage
            });
            using var response = await client.SendAsync(request);
            var dto = response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<ProfileDto>(JsonOptions) : null;
            return new()
            {
                Success = dto is not null,
                Message = dto is not null ? "Profile saved." : "Profile could not be saved.",
                Data = dto is null ? null : Map(dto)
            };
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            return new() { Success = false, Message = "Profile service is unavailable." };
        }
    }

    public async Task<ApiResponse<bool>> CompleteProfileAsync(CompleteProfileRequest request)
    {
        var current = await GetProfileAsync();
        var result = await SaveProfileAsync(new PatientProfile
        {
            FullName = request.FullName,
            Email = current?.Email ?? string.Empty,
            PhoneNumber = request.PhoneNumber,
            DateOfBirth = request.DateOfBirth,
            PreferredGlucoseUnit = request.PreferredGlucoseUnit,
            EnablePushNotifications = request.EnablePushNotifications,
            ThemePreference = current?.ThemePreference ?? "System",
            PreferredLanguage = current?.PreferredLanguage ?? "en"
        });
        return new() { Success = result.Success, Message = result.Message, Data = result.Success };
    }

    private static PatientProfile Map(ProfileDto dto) => new()
    {
        FullName = dto.FullName,
        Email = dto.Email,
        PhoneNumber = dto.PhoneNumber,
        DateOfBirth = dto.DateOfBirth?.ToDateTime(TimeOnly.MinValue),
        PreferredGlucoseUnit = string.Equals(dto.PreferredGlucoseUnit, "mmol/L", StringComparison.OrdinalIgnoreCase)
            ? GlucoseUnit.MmolL : GlucoseUnit.MgDl,
        ThemePreference = dto.Theme,
        PreferredLanguage = dto.Language
    };

    private static async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path)
    {
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");
        var message = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token))
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return message;
    }

    private sealed record ProfileDto(
        string FullName,
        string Email,
        string? PhoneNumber,
        DateOnly? DateOfBirth,
        string PreferredGlucoseUnit,
        bool ProfileCompleted,
        string Theme,
        string Language);
}
