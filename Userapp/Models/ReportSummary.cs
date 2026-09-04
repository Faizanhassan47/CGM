using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class ReportSummary
{
    public string PeriodTitle { get; set; } = string.Empty; // e.g., "Last 14 Days", "Today"
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalReadingsCount { get; set; }
    public double AverageGlucose { get; set; }
    public double StandardDeviation { get; set; }
    public double GlucoseManagementIndicator { get; set; } // GMI %
    public double TimeInRange { get; set; } // % (70-180 mg/dL)
    public double TimeAboveRange { get; set; } // % (>180 mg/dL)
    public double TimeBelowRange { get; set; } // % (<70 mg/dL)
    public double TimeVeryHigh { get; set; } // % (>250 mg/dL)
    public double TimeVeryLow { get; set; } // % (<54 mg/dL)
    public GlucoseUnit Unit { get; set; } = GlucoseUnit.MgDl;
    public List<GlucoseMeasurement> Measurements { get; set; } = new();
}
