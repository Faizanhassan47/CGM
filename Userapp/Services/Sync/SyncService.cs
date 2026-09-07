using System.Text.Json;
using System.Net.Http.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Cgm;

namespace CGM.PatientApp.Services.Sync;

public interface ISyncService
{
    Task EnqueueMeasurementAsync(CgmRawMeasurement measurement);
    Task SyncNowAsync();
}

public class SyncService : ISyncService
{
    private readonly ILocalCacheService _localCache;
    private readonly HttpClient _client;
    private const string QueueKey = "cgm_offline_measurement_queue";
    private readonly System.Timers.Timer _syncTimer;

    public SyncService(ILocalCacheService localCache, HttpClient client)
    {
        _localCache = localCache;
        _client = client;
        
        // Setup background sync every 60 seconds
        _syncTimer = new System.Timers.Timer(60000);
        _syncTimer.Elapsed += async (s, e) => await SyncNowAsync();
        _syncTimer.Start();
    }

    public async Task EnqueueMeasurementAsync(CgmRawMeasurement measurement)
    {
        var queue = await _localCache.GetAsync<List<CgmRawMeasurement>>(QueueKey) ?? new List<CgmRawMeasurement>();
        queue.Add(measurement);
        await _localCache.SetAsync(QueueKey, queue);
        
        // Try to sync immediately if possible
        _ = SyncNowAsync();
    }

    public async Task SyncNowAsync()
    {
        try
        {
            var queue = await _localCache.GetAsync<List<CgmRawMeasurement>>(QueueKey);
            if (queue == null || !queue.Any()) return;

            var token = await SecureStorage.Default.GetAsync("cgm_access_token");
            if (string.IsNullOrWhiteSpace(token)) return;

            var request = new HttpRequestMessage(HttpMethod.Post, "api/glucose/sync-bulk");
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

            using var sensorRequest = new HttpRequestMessage(HttpMethod.Get, "api/sensors/active");
            sensorRequest.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
            using var sensorResponse = await _client.SendAsync(sensorRequest);
            if (!sensorResponse.IsSuccessStatusCode) return;
            var sensor = await sensorResponse.Content.ReadFromJsonAsync<JsonElement>();
            if (!sensor.TryGetProperty("id", out var sensorIdProperty)) return;
            var sensorId = sensorIdProperty.GetInt32();
            
            // Map CgmRawMeasurement to what the API expects
            var measurements = queue.Select(m => new
            {
                SensorId = sensorId,
                SequenceNumber = m.SequenceNumber,
                GlucoseValue = (decimal)m.GlucoseValueMgDl,
                GlucoseUnit = "mg/dL",
                MeasurementTime = DateTimeOffset.FromUnixTimeSeconds(m.Timestamp).UtcDateTime,
                Trend = (string?)null,
                GlucoseStatus = m.GlucoseValueMgDl < 70 ? "Low" : m.GlucoseValueMgDl > 180 ? "High" : "In Range",
                BatteryVoltageMv = (int)m.BatteryVoltageMv,
                DeviceTemperatureC = (decimal)m.TemperatureCelsius,
                WE1CurrentNa = (decimal)m.We1NanoAmps
            }).ToList();

            request.Content = JsonContent.Create(new { SensorId = sensorId, Measurements = measurements });
            var response = await _client.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                // Clear queue on success
                await _localCache.RemoveAsync(QueueKey);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SyncService] Sync failed: {ex.Message}");
        }
    }
}
