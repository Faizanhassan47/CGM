using System.Net.Http.Headers;
using System.Net.Http.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using System.Text.Json;

namespace CGM.PatientApp.Services.Cgm;

public class ApiGlucoseService : IGlucoseService
{
    private readonly HttpClient _client;
    
    public event EventHandler<GlucoseMeasurement>? GlucoseReadingReceived;

    public ApiGlucoseService(HttpClient client)
    {
        _client = client;
    }

    private static async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path)
    {
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<GlucoseSummary> GetDashboardSummaryAsync()
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, "api/glucose/summary");
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var summary = await response.Content.ReadFromJsonAsync<GlucoseSummary>();
                if (summary != null) return summary;
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiGlucoseService] GetDashboardSummaryAsync failed: {ex}");
        }
        
        return new GlucoseSummary(); // Empty state
    }

    public async Task<GlucoseMeasurement?> GetLatestReadingAsync()
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, "api/glucose/history?limit=1");
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var readings = await response.Content.ReadFromJsonAsync<List<GlucoseMeasurement>>();
                return readings?.FirstOrDefault();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiGlucoseService] GetLatestReadingAsync failed: {ex}");
        }
        return null;
    }

    public async Task<IReadOnlyList<GlucoseMeasurement>> GetRecentReadingsAsync(TimeSpan timeSpan)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, $"api/glucose/history?hours={timeSpan.TotalHours}");
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var readings = await response.Content.ReadFromJsonAsync<List<GlucoseMeasurement>>();
                return readings ?? new List<GlucoseMeasurement>();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiGlucoseService] GetRecentReadingsAsync failed: {ex}");
        }
        return new List<GlucoseMeasurement>();
    }

    public void NotifyReadingReceived(GlucoseMeasurement measurement)
    {
        GlucoseReadingReceived?.Invoke(this, measurement);
    }
}
