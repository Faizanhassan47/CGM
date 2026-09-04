using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class AlertSettings
{
    public bool EnableLowGlucoseAlert { get; set; } = true;
    public double LowGlucoseThreshold { get; set; } = 70.0; // mg/dL

    public bool EnableHighGlucoseAlert { get; set; } = true;
    public double HighGlucoseThreshold { get; set; } = 180.0; // mg/dL

    public bool EnableUrgentLowAlert { get; set; } = true;
    public double UrgentLowThreshold { get; set; } = 54.0; // mg/dL

    public bool EnableRapidRiseAlert { get; set; } = true;
    public bool EnableRapidFallAlert { get; set; } = true;

    public bool EnableNoDataAlert { get; set; } = true;
    public int NoDataTimeoutMinutes { get; set; } = 20;

    public bool EnableSensorEndingAlert { get; set; } = true;
    public bool EnableLowBatteryAlert { get; set; } = true;

    public GlucoseUnit Unit { get; set; } = GlucoseUnit.MgDl;
}
