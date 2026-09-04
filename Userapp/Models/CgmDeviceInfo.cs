using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class CgmDeviceInfo
{
    public string BluetoothId { get; set; } = string.Empty;
    public int Id { get; set; }
    public string ModelCode { get; set; } = string.Empty;
    public string DeviceName { get; set; } = string.Empty;
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public ushort BatteryVoltageMv { get; set; }
    public string BatteryStatusText { get; set; } = "Good";
    public double TemperatureCelsius { get; set; }
    public int? Rssi { get; set; }
    public bool AdvertisesCgmService { get; set; }
    public CgmConnectionState ConnectionState { get; set; } = CgmConnectionState.Disconnected;
    public DateTime? LastCommunicationTime { get; set; }
    public bool IsDataStale => !LastCommunicationTime.HasValue || DateTime.UtcNow - LastCommunicationTime.Value > TimeSpan.FromMinutes(10);
    public CgmDeviceType DeviceType { get; set; } = CgmDeviceType.Disposable;
    public string MaskedSerialNumber => SerialNumber.Length > 4 
        ? new string('*', SerialNumber.Length - 4) + SerialNumber[^4..] 
        : SerialNumber;
}
