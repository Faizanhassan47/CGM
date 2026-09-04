namespace CGM.PatientApp.Services.Ble;

public static class CgmDeviceFilter
{
    public static bool IsCandidateName(string? deviceName) =>
        !string.IsNullOrWhiteSpace(deviceName) && deviceName.StartsWith("CGM-", StringComparison.OrdinalIgnoreCase);
}
