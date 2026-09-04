using CGM.PatientApp.Enums;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface ICgmDeviceService
{
    event EventHandler<CgmRawMeasurement>? RawMeasurementReceived;
    event EventHandler<CgmConnectionState>? ConnectionStateChanged;
    
    CgmConnectionState State { get; }
    CgmDeviceInfo? ConnectedDevice { get; }
    Task<CgmDeviceInfo?> ConnectAndVerifyAsync(string bluetoothId, string advertisedName, CancellationToken cancellationToken = default);
    Task<bool> InitializeAsync();
    Task<string> ReadFirmwareVersionAsync();
    Task<string> ReadSerialNumberAsync();
    Task<ushort> ReadBatteryVoltageAsync();
    Task<double> ReadDeviceTemperatureAsync();
    Task<(bool isMeasuring, ushort latestSn)> ReadMeasurementStateAsync();
    Task<bool> StartMeasurementAsync();
    Task<IReadOnlyList<CgmRawMeasurement>> ReadHistoricalDataAsync(ushort startSn, ushort endSn);
}
