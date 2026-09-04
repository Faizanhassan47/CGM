namespace CGM.PatientApp.Models;

public sealed record FirmwareVersionResult(string FirmwareVersion);
public sealed record DeviceSerialNumberResult(string SerialNumber)
{
    public string MaskedSerialNumber => SerialNumber.Length > 4
        ? new string('*', SerialNumber.Length - 4) + SerialNumber[^4..]
        : SerialNumber;
}
public sealed record BatteryVoltageResult(ushort BatteryVoltageMv);
public sealed record DeviceTemperatureResult(short RawValue)
{
    public double DeviceTemperatureC => RawValue / 10.0;
}
public sealed record MeasurementStateResult(bool IsMeasuring, uint ElapsedMeasurementTime, ushort LatestSequenceNumber);
