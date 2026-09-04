using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface ICgmProtocolParser
{
    byte CalculateChecksum(ReadOnlySpan<byte> frame);
    bool ValidateFrame(ReadOnlySpan<byte> frame);
    byte[] BuildCommand(byte cmd, ReadOnlySpan<byte> data);
    FirmwareVersionResult? ParseFirmwareVersion(ReadOnlySpan<byte> frame);
    DeviceSerialNumberResult? ParseDeviceSerialNumber(ReadOnlySpan<byte> frame);
    BatteryVoltageResult? ParseBatteryResult(ReadOnlySpan<byte> frame);
    DeviceTemperatureResult? ParseDeviceTemperatureResult(ReadOnlySpan<byte> frame);
    MeasurementStateResult? ParseMeasurementStateResult(ReadOnlySpan<byte> frame);
    
    CgmRawMeasurement? ParseLiveMeasurement(ReadOnlySpan<byte> frame);
    (bool isMeasuring, uint measurementTime, ushort latestSn)? ParseMeasurementStatus(ReadOnlySpan<byte> frame);
    string? ParseAsciiString(ReadOnlySpan<byte> frame);
    ushort? ParseBatteryVoltage(ReadOnlySpan<byte> frame);
    double? ParseTemperature(ReadOnlySpan<byte> frame);
    (bool isMeasuring, ushort latestSn)? ParseMeasurementState(ReadOnlySpan<byte> frame);
    IReadOnlyList<CgmRawMeasurement> ParseHistoricalMeasurements(ReadOnlySpan<byte> frame);
}
