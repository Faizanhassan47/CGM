namespace CGM.PatientApp.Services.Ble;

public static class CgmDeviceFilter
{
    public static bool IsCandidateName(string? deviceName) =>
        !string.IsNullOrWhiteSpace(deviceName) && deviceName.StartsWith("CGM-", StringComparison.OrdinalIgnoreCase);

    public static bool IsSimulator(string? deviceName, bool advertisesService, bool allowSimulator) =>
        allowSimulator && advertisesService && !IsCandidateName(deviceName);
}
