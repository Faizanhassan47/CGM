using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Services;

public sealed class NoOpForegroundMonitoringService : IForegroundMonitoringService
{
    public void StartService(string deviceName = "CGM Sensor") { }
    public void StopService() { }
    public void UpdateReading(string deviceName, double glucoseMgDl, string? trend = null) { }
}
