namespace CGM.PatientApp.Models;

public class Family
{
    public int FamilyId { get; set; }
    public string FamilyName { get; set; } = string.Empty;
    public int OwnerUserId { get; set; }
    public int CurrentUserId { get; set; }
    public bool IsOwner { get; set; }
    public string MyReferralCode { get; set; } = string.Empty;
    public double LowGlucoseThreshold { get; set; } = 70;
    public double HighGlucoseThreshold { get; set; } = 180;
    public List<FamilyMember> Members { get; set; } = new();
}

public class FamilyMember
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool ReceiveAlerts { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class WeeklyReportItem
{
    public int UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string AlertType { get; set; } = string.Empty;
    public decimal? GlucoseValue { get; set; }
    public DateTime TriggeredAt { get; set; }
}
