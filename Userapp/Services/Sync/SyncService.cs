using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Diagnostics;
using CGM.PatientApp.Services.Api;

namespace CGM.PatientApp.Services.Sync;

public interface ISyncService
{
    Task EnqueueMeasurementAsync(CgmRawMeasurement measurement, CancellationToken cancellationToken = default);
    Task SyncNowAsync(CancellationToken cancellationToken = default);
}

public sealed class SyncService : ISyncService, IDisposable
{
    private const int BatchSize = 100;
    private readonly ILocalMeasurementRepository _measurementRepo;
    private readonly HttpClient _client;
    private readonly ICrashReporter _crashReporter;
    private readonly SemaphoreSlim _syncLock = new(1, 1);
    private readonly PeriodicTimer _timer = new(TimeSpan.FromMinutes(1));
    private readonly CancellationTokenSource _lifetime = new();
    private readonly Task _schedulerTask;
    private bool _disposed;

    public SyncService(ILocalMeasurementRepository measurementRepo, HttpClient client, ICrashReporter crashReporter)
    {
        _measurementRepo = measurementRepo;
        _client = client;
        _crashReporter = crashReporter;
        _schedulerTask = RunSchedulerAsync(_lifetime.Token);
    }

    public async Task EnqueueMeasurementAsync(CgmRawMeasurement measurement, CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        await _syncLock.WaitAsync(cancellationToken);
        try
        {
            var entity = new LocalMeasurement
            {
                SequenceNumber = measurement.SequenceNumber,
                GlucoseValue = measurement.GlucoseValueMgDl,
                MeasuredAt = measurement.ReceivedTime.Kind == DateTimeKind.Utc ? measurement.ReceivedTime : measurement.ReceivedTime.ToUniversalTime(),
                SyncStatus = "Pending",
                BatteryVoltageMv = (int)measurement.BatteryVoltageMv,
                DeviceTemperatureC = measurement.TemperatureCelsius,
                We1NanoAmps = measurement.We1NanoAmps
            };

            await _measurementRepo.SaveMeasurementAsync(entity);
        }
        finally { _syncLock.Release(); }

        SafeAsync.Run(() => SyncNowAsync(_lifetime.Token), "SyncService.Enqueue");
    }

    public async Task SyncNowAsync(CancellationToken cancellationToken = default)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (!await _syncLock.WaitAsync(0, cancellationToken)) return;
        try
        {
            var batch = await _measurementRepo.GetPendingMeasurementsAsync(BatchSize);
            if (batch.Count == 0) return;

            var token = await SecureStorage.Default.GetAsync("cgm_access_token");
            if (string.IsNullOrWhiteSpace(token)) return;

            var sensorId = await GetActiveSensorIdAsync(token, cancellationToken);
            if (!sensorId.HasValue) return;

            using var request = new HttpRequestMessage(HttpMethod.Post, ApiEndpoints.Glucose.BulkSync);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Content = JsonContent.Create(new
            {
                SensorId = sensorId.Value,
                Measurements = batch.Select(x => new
                {
                    SensorId = sensorId.Value,
                    x.SequenceNumber,
                    GlucoseValue = (decimal)x.GlucoseValue,
                    GlucoseUnit = "mg/dL",
                    MeasurementTime = x.MeasuredAt,
                    Trend = x.Trend,
                    GlucoseStatus = x.GlucoseValue < 70 ? "Low" : x.GlucoseValue > 180 ? "High" : "In Range",
                    BatteryVoltageMv = x.BatteryVoltageMv,
                    DeviceTemperatureC = (decimal)x.DeviceTemperatureC,
                    WE1CurrentNa = (decimal)x.We1NanoAmps
                }).ToList()
            });

            using var response = await _client.SendAsync(request, cancellationToken);
            if (response.IsSuccessStatusCode)
            {
                await _measurementRepo.MarkAsSyncedAsync(batch.Select(x => x.Id));
            }
            else
            {
                var error = $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                foreach (var item in batch)
                {
                    await _measurementRepo.MarkAsFailedAsync(item.Id, error);
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            try
            {
                var batch = await _measurementRepo.GetPendingMeasurementsAsync(BatchSize);
                foreach (var item in batch)
                {
                    await _measurementRepo.MarkAsFailedAsync(item.Id, exception.Message);
                }
            }
            catch { }
            await _crashReporter.ReportAsync(exception, "SyncService.SyncNow");
        }
        finally { _syncLock.Release(); }
    }

    private async Task RunSchedulerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(cancellationToken))
                await SyncNowAsync(cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception) { await _crashReporter.ReportAsync(exception, "SyncService.Scheduler"); }
    }

    private async Task<int?> GetActiveSensorIdAsync(string token, CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, ApiEndpoints.Sensors.Active);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var response = await _client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            var sensor = await response.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
            return sensor.TryGetProperty("id", out var id) && id.TryGetInt32(out var value) ? value : null;
        }

        // Auto-provision an active sensor record if none exists yet so offline/BLE telemetry syncs seamlessly
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            try
            {
                using var startReq = new HttpRequestMessage(HttpMethod.Post, ApiEndpoints.Sensors.Start);
                startReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
                startReq.Content = JsonContent.Create(new { WarmupMinutes = 0, SensorIdentifier = "CGM-ACTIVE-001" });
                using var startRes = await _client.SendAsync(startReq, cancellationToken);
                if (startRes.IsSuccessStatusCode)
                {
                    var started = await startRes.Content.ReadFromJsonAsync<JsonElement>(cancellationToken);
                    return started.TryGetProperty("id", out var sid) && sid.TryGetInt32(out var sval) ? sval : null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[SyncService] Auto-start sensor failed: {ex}");
            }
        }

        return null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _lifetime.Cancel();
        _timer.Dispose();
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }
}
