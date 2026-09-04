using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class GlucoseSummary
{
    public GlucoseMeasurement? LatestReading { get; set; }
    public double TimeInRangePercentage { get; set; }
    public double TimeAboveRangePercentage { get; set; }
    public double TimeBelowRangePercentage { get; set; }
    public double AverageGlucose { get; set; }
    public double HighestGlucose { get; set; }
    public double LowestGlucose { get; set; }
    public GlucoseUnit Unit { get; set; } = GlucoseUnit.MgDl;
    public DateTime LastSyncTime { get; set; } = DateTime.UtcNow;
    public string SensorStatusText { get; set; } = "Active";
    public string BatteryStatusText { get; set; } = "Good";
}
