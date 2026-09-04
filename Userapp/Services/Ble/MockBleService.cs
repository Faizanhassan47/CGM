using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Ble;

public class MockBleService : IBleService
{
    private CgmConnectionState _currentState = CgmConnectionState.Disconnected;
    public CgmConnectionState CurrentState => _currentState;
    public CgmBleDiagnostics Diagnostics { get; } = new()
    {
        ServiceFff0Found = true, CharacteristicFff1Found = true, CharacteristicFff2Found = true
    };

    public event EventHandler<CgmConnectionState>? ConnectionStateChanged;
    public event EventHandler<byte[]>? NotificationReceived;

    private void SetState(CgmConnectionState newState)
    {
        _currentState = newState;
        ConnectionStateChanged?.Invoke(this, newState);
    }

    public Task<bool> CheckBluetoothEnabledAsync()
    {
        return Task.FromResult(true);
    }

    public Task<bool> RequestPermissionsAsync()
    {
        return Task.FromResult(true);
    }

    public async Task StartScanningAsync(Action<CgmDeviceInfo> onDeviceDiscovered, CancellationToken cancellationToken = default)
    {
        SetState(CgmConnectionState.Scanning);

        // Simulate discovering CGM device after 2 seconds
        await Task.Delay(2000, cancellationToken);

        if (!cancellationToken.IsCancellationRequested)
        {
            var discoveredDevice = new CgmDeviceInfo
            {
                DeviceName = "CGM-C221",
                BluetoothId = "mock-cgm-c221",
                Rssi = -58,
                SerialNumber = "26082400C221",
                FirmwareVersion = "A100",
                BatteryVoltageMv = 2850,
                BatteryStatusText = "Good",
                TemperatureCelsius = 32.4,
                ConnectionState = CgmConnectionState.DeviceFound,
                LastCommunicationTime = DateTime.UtcNow
            };

            SetState(CgmConnectionState.DeviceFound);
            onDeviceDiscovered?.Invoke(discoveredDevice);
        }
    }

    public Task StopScanningAsync()
    {
        if (_currentState == CgmConnectionState.Scanning)
        {
            SetState(CgmConnectionState.Disconnected);
        }
        return Task.CompletedTask;
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        SetState(CgmConnectionState.Connecting);
        await Task.Delay(1000, cancellationToken);
        SetState(CgmConnectionState.Connected);
        return true;
    }

    public Task DisconnectAsync()
    {
        SetState(CgmConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    public Task<bool> WriteCommandAsync(byte[] commandBytes, CancellationToken cancellationToken = default)
    {
        if (commandBytes.Length < 3) return Task.FromResult(false);
        byte[] payload = commandBytes[2] switch
        {
            0xE7 => System.Text.Encoding.ASCII.GetBytes("A100"),
            0xE8 => System.Text.Encoding.ASCII.GetBytes("26082400C221"),
            0xE1 => [0x22, 0x0B],
            0xE2 => [0x44, 0x01],
            0xE3 => [0x01, 0x10, 0x0E, 0x00, 0x00, 0xE2, 0x04],
            _ => []
        };
        if (payload.Length == 0) return Task.FromResult(false);
        var response = new byte[payload.Length + 4];
        response[0] = 0xAA; response[1] = (byte)response.Length; response[2] = commandBytes[2];
        payload.CopyTo(response, 3);
        response[^1] = (byte)(-response[..^1].Sum(x => x) & 0xFF);
        Task.Run(() => NotificationReceived?.Invoke(this, response), cancellationToken);
        return Task.FromResult(true);
    }

    public void SimulateNotification(byte[] rawBytes)
    {
        NotificationReceived?.Invoke(this, rawBytes);
    }
}
