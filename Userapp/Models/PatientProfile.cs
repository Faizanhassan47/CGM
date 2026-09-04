using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class PatientProfile
{
    public string UserId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public GlucoseUnit PreferredGlucoseUnit { get; set; } = GlucoseUnit.MgDl;
    public bool EnablePushNotifications { get; set; } = true;
    public bool EnableCriticalAlerts { get; set; } = true;
    public string ThemePreference { get; set; } = "System"; // Light, Dark, System
    public string PreferredLanguage { get; set; } = "en";
    public double TargetGlucoseMin { get; set; } = 70.0; // mg/dL default or equivalent
    public double TargetGlucoseMax { get; set; } = 180.0;
}
