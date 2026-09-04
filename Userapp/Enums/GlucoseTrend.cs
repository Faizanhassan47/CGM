namespace CGM.PatientApp.Enums;

public enum GlucoseTrend
{
    None = 0,
    RisingRapidly = 1,  // ↑
    Rising = 2,         // ↗
    Stable = 3,         // →
    Falling = 4,        // ↘
    FallingRapidly = 5   // ↓
}
