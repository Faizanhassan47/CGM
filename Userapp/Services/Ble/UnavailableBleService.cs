using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Ble;

/// <summary>
/// Safe production fallback until an approved CGM BLE SDK/protocol is supplied.
/// It never fabricates a device, connection, or clinical reading.
/// </summary>
public sealed class UnavailableBleService : IBleService
{
    public event EventHandler<CgmConnectionState>? ConnectionStateChanged;
    public event EventHandler<byte[]>? NotificationReceived;
    public CgmConnectionState CurrentState { get; private set; } = CgmConnectionState.Disconnected;
    public CgmBleDiagnostics Diagnostics { get; } = new() { LastError = "Bluetooth is unavailable on this platform." };

    public Task<bool> CheckBluetoothEnabledAsync() => Task.FromResult(false);
    public Task<bool> RequestPermissionsAsync() => Task.FromResult(false);

    public Task StartScanningAsync(Action<CgmDeviceInfo> onDeviceDiscovered, CancellationToken cancellationToken = default)
    {
        SetState(CgmConnectionState.ConnectionFailed);
        return Task.CompletedTask;
    }

    public Task StopScanningAsync() => Task.CompletedTask;

    public Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        SetState(CgmConnectionState.ConnectionFailed);
        return Task.FromResult(false);
    }

    public Task DisconnectAsync()
    {
        SetState(CgmConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    public Task<bool> WriteCommandAsync(byte[] commandBytes, CancellationToken cancellationToken = default) => Task.FromResult(false);

    private void SetState(CgmConnectionState state)
    {
        CurrentState = state;
        ConnectionStateChanged?.Invoke(this, state);
    }
}
