using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Device;

public sealed class ApiDeviceService(HttpClient client) : IDeviceService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<IReadOnlyList<SupportedCgmDevice>> GetSupportedDevicesAsync() =>
        Task.FromResult<IReadOnlyList<SupportedCgmDevice>>(
        [
            new() { Id = "DEV-G-AA", Name = "Disposable CGM", ModelCode = "G-AA", Description = "All-in-one sensor for up to 14 days.", DeviceType = CgmDeviceType.Disposable, IconResource = "device_disposable.png", PreparationInstructions = "Follow the Instructions for Use supplied with your G-AA CGM.", IsRecommended = true },
            new() { Id = "DEV-G-PA", Name = "Reusable CGM", ModelCode = "G-PA0 / G-PA1", Description = "Rechargeable transmitter with replaceable sensors.", DeviceType = CgmDeviceType.Reusable, IconResource = "device_reusable.png", PreparationInstructions = "Follow the Instructions for Use supplied with your G-PA transmitter." }
        ]);

    public async Task<CgmDeviceInfo?> GetConfiguredDeviceAsync()
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, "api/devices");
            using var response = await client.SendAsync(request);
            if (!response.IsSuccessStatusCode) return null;
            var devices = await response.Content.ReadFromJsonAsync<List<DeviceDto>>(JsonOptions);
            return devices?.Select(Map).FirstOrDefault();
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException) { return null; }
    }

    public async Task SaveConfiguredDeviceAsync(CgmDeviceInfo device)
    {
        using var request = await AuthorizedAsync(HttpMethod.Post, "api/devices/register");
        request.Content = JsonContent.Create(new
        {
            device.DeviceName,
            DeviceType = device.DeviceType.ToString(),
            DeviceModel = string.IsNullOrWhiteSpace(device.ModelCode) ? device.DeviceType == CgmDeviceType.Reusable ? "G-PA" : "G-AA" : device.ModelCode,
            device.SerialNumber,
            BleDeviceName = device.DeviceName,
            device.FirmwareVersion,
            BatteryVoltageMv = (int?)device.BatteryVoltageMv
        });
        using var response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        try
        {
            var registeredDevice = await response.Content.ReadFromJsonAsync<DeviceDto>(JsonOptions);
            if (registeredDevice is not null)
            {
                using var activeSensorReq = await AuthorizedAsync(HttpMethod.Get, "api/sensors/active");
                using var activeSensorRes = await client.SendAsync(activeSensorReq);
                if (!activeSensorRes.IsSuccessStatusCode)
                {
                    using var startSensorReq = await AuthorizedAsync(HttpMethod.Post, "api/sensors/start");
                    startSensorReq.Content = JsonContent.Create(new
                    {
                        DeviceId = registeredDevice.Id,
                        SensorIdentifier = string.IsNullOrWhiteSpace(device.SerialNumber) ? null : $"SN-{device.SerialNumber}",
                        WarmupMinutes = 60
                    });
                    await client.SendAsync(startSensorReq);
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiDeviceService] Auto-start sensor failed: {ex}");
        }

        Preferences.Default.Set("cgm_device_configured", true);
    }

    public async Task RemoveConfiguredDeviceAsync()
    {
        var configured = await GetConfiguredDeviceAsync();
        if (configured is not null)
        {
            using var request = await AuthorizedAsync(HttpMethod.Delete, $"api/devices/{configured.Id}");
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        Preferences.Default.Set("cgm_device_configured", false);
    }

    private static CgmDeviceInfo Map(DeviceDto dto) => new()
    {
        Id = dto.Id,
        DeviceName = dto.DeviceName,
        SerialNumber = dto.SerialNumber ?? string.Empty,
        FirmwareVersion = dto.FirmwareVersion ?? string.Empty,
        BatteryVoltageMv = (ushort)Math.Clamp(dto.BatteryVoltageMv ?? 0, 0, ushort.MaxValue),
        BatteryStatusText = dto.BatteryPercentage is null ? "Unknown" : $"{dto.BatteryPercentage:0}%",
        ConnectionState = Enum.TryParse<CgmConnectionState>(dto.ConnectionStatus, true, out var state) ? state : CgmConnectionState.Disconnected,
        LastCommunicationTime = dto.LastConnectedAt,
        DeviceType = string.Equals(dto.DeviceType, "Reusable", StringComparison.OrdinalIgnoreCase) ? CgmDeviceType.Reusable : CgmDeviceType.Disposable,
        ModelCode = dto.DeviceModel
    };

    private static async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path)
    {
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token)) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    private sealed record DeviceDto(int Id, string DeviceName, string DeviceType, string DeviceModel, string? SerialNumber,
        string? BleDeviceName, string? FirmwareVersion, int? BatteryVoltageMv, decimal? BatteryPercentage,
        string ConnectionStatus, DateTime? LastConnectedAt);
}
