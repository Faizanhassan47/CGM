using CGM.PatientApp.Enums;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IBleService
{
    event EventHandler<CgmConnectionState>? ConnectionStateChanged;
    CgmConnectionState CurrentState { get; }
    CgmBleDiagnostics Diagnostics { get; }
    
    Task<bool> CheckBluetoothEnabledAsync();
    Task<bool> RequestPermissionsAsync();
    Task StartScanningAsync(Action<CgmDeviceInfo> onDeviceDiscovered, CancellationToken cancellationToken = default);
    Task StopScanningAsync();
    Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default);
    Task DisconnectAsync();
    Task<bool> WriteCommandAsync(byte[] commandBytes, CancellationToken cancellationToken = default);
    event EventHandler<byte[]>? NotificationReceived;
}
