namespace CGM.PatientApp.Enums;

public enum AlertCategory
{
    LowGlucose = 0,
    HighGlucose = 1,
    RapidRise = 2,
    RapidFall = 3,
    NoData = 4,
    SensorEnding = 5,
    SensorExpired = 6,
    CgmBatteryLow = 7,
    ConnectionLost = 8
}
