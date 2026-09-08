using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Family;

public sealed class ApiFamilyService(HttpClient client) : IFamilyService
{
    public async Task<CGM.PatientApp.Models.Family?> GetAsync()
    {
        using var response = await SendAsync(HttpMethod.Get, "api/family");
        if (response.StatusCode == HttpStatusCode.NotFound) return null;
        await EnsureAsync(response);
        return await response.Content.ReadFromJsonAsync<CGM.PatientApp.Models.Family>();
    }
    public Task<CGM.PatientApp.Models.Family> CreateAsync(string name) => SendFamilyAsync("api/family/create", new { familyName = name });
    public Task<CGM.PatientApp.Models.Family> JoinAsync(string code) => SendFamilyAsync("api/family/join", new { referralCode = code });
    public async Task SetAlertsAsync(int userId, bool enabled) { using var r = await SendAsync(HttpMethod.Put, $"api/family/members/{userId}/alerts", new { receiveAlerts = enabled }); await EnsureAsync(r); }
    public async Task RemoveAsync(int userId) { using var r = await SendAsync(HttpMethod.Delete, $"api/family/members/{userId}"); await EnsureAsync(r); }
    public async Task LeaveAsync() { using var r = await SendAsync(HttpMethod.Post, "api/family/leave"); await EnsureAsync(r); }
    public Task<CGM.PatientApp.Models.Family> UpdateThresholdsAsync(double low, double high) => SendFamilyPutAsync("api/family/thresholds", new { lowGlucoseThreshold = low, highGlucoseThreshold = high });
    private async Task<CGM.PatientApp.Models.Family> SendFamilyAsync(string path, object body) { using var r = await SendAsync(HttpMethod.Post, path, body); await EnsureAsync(r); return (await r.Content.ReadFromJsonAsync<CGM.PatientApp.Models.Family>())!; }
    private async Task<CGM.PatientApp.Models.Family> SendFamilyPutAsync(string path, object body) { using var r = await SendAsync(HttpMethod.Put, path, body); await EnsureAsync(r); return (await r.Content.ReadFromJsonAsync<CGM.PatientApp.Models.Family>())!; }
    private async Task<HttpResponseMessage> SendAsync(HttpMethod method, string path, object? body = null)
    {
        var request = new HttpRequestMessage(method, path);
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (body != null) request.Content = JsonContent.Create(body);
        return await client.SendAsync(request);
    }
    private static async Task EnsureAsync(HttpResponseMessage response)
    {
        if (response.IsSuccessStatusCode) return;
        var json = await response.Content.ReadAsStringAsync();
        try { throw new InvalidOperationException(JsonDocument.Parse(json).RootElement.GetProperty("message").GetString()); }
        catch (KeyNotFoundException) { throw new InvalidOperationException("Unable to complete the family request."); }
    }
}
