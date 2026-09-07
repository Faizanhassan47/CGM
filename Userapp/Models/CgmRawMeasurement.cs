namespace CGM.PatientApp.Models;

public class CgmRawMeasurement
{
    public uint Timestamp { get; set; }
    public ushort SequenceNumber { get; set; }
    public ushort BatteryVoltageMv { get; set; }
    public short RawTemperature { get; set; }
    public double TemperatureCelsius => RawTemperature / 10.0;
    public short RawWe1 { get; set; }
    public double We1NanoAmps => RawWe1 / 100.0;
    /// <summary>
    /// Development simulator convention only: B0 WE1 contains mg/dL multiplied by 100.
    /// Real hardware requires its validated calibration algorithm and must not use this value.
    /// </summary>
    public double GlucoseValueMgDl => RawWe1 / 100.0;
    public byte[]? ReservedBytes { get; set; }
    public DateTime ReceivedTime { get; set; } = DateTime.UtcNow;
}
