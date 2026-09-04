using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class GlucoseMeasurement
{
    public ushort SequenceNumber { get; set; }
    public double GlucoseValue { get; set; }
    public GlucoseUnit Unit { get; set; } = GlucoseUnit.MgDl;
    public DateTime MeasurementTime { get; set; }
    public GlucoseTrend Trend { get; set; } = GlucoseTrend.Stable;
    public GlucoseStatus Status { get; set; } = GlucoseStatus.Normal;
    public string? Notes { get; set; }
}
