using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Cgm;

public class ApiSensorService : ISensorService
{
    private readonly HttpClient _client;

    public ApiSensorService(HttpClient client)
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

    public async Task<SensorInfo> GetCurrentSensorInfoAsync()
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, "api/sensors/active");
            using var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<SensorInfo>();
        }
        catch 
        {
            return null;
        }
    }

    public async Task<bool> StartSensorSessionAsync(string sensorId)
    {
        try
        {
            using var devicesReq = await AuthorizedAsync(HttpMethod.Get, "api/devices");
            using var devicesRes = await _client.SendAsync(devicesReq);
            if (!devicesRes.IsSuccessStatusCode) return false;

            var devices = await devicesRes.Content.ReadFromJsonAsync<List<JsonElement>>();
            var device = devices?.FirstOrDefault(d => d.GetProperty("serialNumber").GetString() == sensorId);
            if (device == null || device.Value.ValueKind == JsonValueKind.Undefined) return false;
            
            int deviceId = device.Value.GetProperty("id").GetInt32();

            using var request = await AuthorizedAsync(HttpMethod.Post, "api/sensors/start");
            request.Content = JsonContent.Create(new
            {
                DeviceId = deviceId,
                SensorIdentifier = sensorId,
                WarmupMinutes = 60
            });
            using var response = await _client.SendAsync(request);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CGM] StartSensorSessionAsync failed: {ex.Message}");
            return false;
        }
    }

    public async Task<bool> StopSensorSessionAsync()
    {
        return await Task.FromResult(true);
    }
}
