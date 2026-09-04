using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class Alert
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public AlertCategory Category { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public double? GlucoseValue { get; set; }
    public GlucoseUnit? Unit { get; set; }
    public bool IsRead { get; set; }
    public bool IsCritical { get; set; }
}
