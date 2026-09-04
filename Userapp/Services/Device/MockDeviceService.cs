using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Device;

public class MockDeviceService : IDeviceService
{
    private const string SelectedDeviceKey = "cgm_selected_device_model";
    private const string ConfiguredDeviceSnKey = "cgm_device_sn";
    private const string ConfiguredDeviceNameKey = "cgm_device_name";

    private readonly List<SupportedCgmDevice> _supportedDevices = new()
    {
        new SupportedCgmDevice
        {
            Id = "DEV-G-AA",
            Name = "Disposable CGM",
            ModelCode = "G-AA",
            Description = "All-in-one integrated continuous glucose sensor with 14-day wear capability.",
            DeviceType = CgmDeviceType.Disposable,
            IconResource = "device_disposable.png",
            PreparationInstructions = "Follow the official Instructions for Use supplied with your G-AA CGM.",
            IsRecommended = true
        },
        new SupportedCgmDevice
        {
            Id = "DEV-G-PA",
            Name = "Reusable Transmitter CGM",
            ModelCode = "G-PA0 / G-PA1",
            Description = "Reusable transmitter with replaceable sensor probe and rechargeable battery.",
            DeviceType = CgmDeviceType.Reusable,
            IconResource = "device_reusable.png",
            PreparationInstructions = "Follow the official Instructions for Use supplied with your G-PA transmitter.",
            IsRecommended = false
        }
    };

    public Task<IReadOnlyList<SupportedCgmDevice>> GetSupportedDevicesAsync()
    {
        return Task.FromResult<IReadOnlyList<SupportedCgmDevice>>(_supportedDevices);
    }

    public Task<CgmDeviceInfo?> GetConfiguredDeviceAsync()
    {
        var sn = Preferences.Default.Get(ConfiguredDeviceSnKey, string.Empty);
        if (string.IsNullOrEmpty(sn))
            return Task.FromResult<CgmDeviceInfo?>(null);

        var name = Preferences.Default.Get(ConfiguredDeviceNameKey, "CGM Device");
        var model = Preferences.Default.Get(SelectedDeviceKey, "G-AA");

        var info = new CgmDeviceInfo
        {
            DeviceName = name,
            SerialNumber = sn,
            FirmwareVersion = "A100",
            BatteryVoltageMv = 2850,
            BatteryStatusText = "Good",
            TemperatureCelsius = 32.4,
            ConnectionState = CgmConnectionState.Connected,
            LastCommunicationTime = DateTime.UtcNow,
            DeviceType = model.Contains("PA") ? CgmDeviceType.Reusable : CgmDeviceType.Disposable
        };

        return Task.FromResult<CgmDeviceInfo?>(info);
    }

    public Task SaveConfiguredDeviceAsync(CgmDeviceInfo device)
    {
        Preferences.Default.Set(ConfiguredDeviceSnKey, device.SerialNumber);
        Preferences.Default.Set(ConfiguredDeviceNameKey, device.DeviceName);
        Preferences.Default.Set("cgm_device_configured", true);
        return Task.CompletedTask;
    }

    public Task RemoveConfiguredDeviceAsync()
    {
        Preferences.Default.Remove(ConfiguredDeviceSnKey);
        Preferences.Default.Remove(ConfiguredDeviceNameKey);
        Preferences.Default.Set("cgm_device_configured", false);
        return Task.CompletedTask;
    }
}
