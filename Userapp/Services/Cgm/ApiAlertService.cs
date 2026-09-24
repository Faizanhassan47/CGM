using System.Net.Http.Headers;
using System.Net.Http.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Services.Cgm;

public class ApiAlertService : IAlertService
{
    private readonly HttpClient _client;
    
    public event EventHandler<Alert>? AlertTriggered;

    public ApiAlertService(HttpClient client)
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

    public async Task<IReadOnlyList<Alert>> GetAlertsAsync(bool unreadOnly = false)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, $"api/alerts{(unreadOnly ? "?unreadOnly=true" : "")}");
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var dtos = await response.Content.ReadFromJsonAsync<List<BackendAlertDto>>();
                if (dtos != null)
                {
                    return dtos.Select(d => new Alert
                    {
                        Id = d.Id ?? Guid.NewGuid().ToString(),
                        Category = (AlertCategory)d.Category,
                        Title = d.Title ?? string.Empty,
                        Message = d.Message ?? string.Empty,
                        Timestamp = d.Timestamp,
                        GlucoseValue = d.GlucoseValue,
                        Unit = d.Unit.HasValue ? (GlucoseUnit)d.Unit.Value : GlucoseUnit.MgDl,
                        IsRead = d.IsRead,
                        IsCritical = d.IsCritical
                    }).ToList();
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiAlertService] GetAlertsAsync failed: {ex}");
        }
        return new List<Alert>();
    }

    public async Task MarkAlertAsReadAsync(string alertId)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Put, $"api/alerts/{alertId}/read");
            await _client.SendAsync(request);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiAlertService] MarkAlertAsReadAsync failed: {ex}");
        }
    }

    public async Task ClearAllAlertsAsync()
    {
        // Backend does not have a clear all endpoint yet, so this might be local only or we iterate.
        var alerts = await GetAlertsAsync(true);
        foreach (var alert in alerts)
        {
            await MarkAlertAsReadAsync(alert.Id);
        }
    }

    public async Task<AlertSettings> GetAlertSettingsAsync()
    {
        // Currently stored locally or as part of patient profile. 
        return new AlertSettings();
    }

    public async Task<bool> UpdateAlertSettingsAsync(AlertSettings settings)
    {
        return true;
    }

    public void NotifyAlertTriggered(Alert alert)
    {
        AlertTriggered?.Invoke(this, alert);
    }

    private sealed class BackendAlertDto
    {
        public string? Id { get; set; }
        public int Category { get; set; }
        public string? Title { get; set; }
        public string? Message { get; set; }
        public DateTime Timestamp { get; set; }
        public double? GlucoseValue { get; set; }
        public int? Unit { get; set; }
        public bool IsRead { get; set; }
        public bool IsCritical { get; set; }
    }
}
