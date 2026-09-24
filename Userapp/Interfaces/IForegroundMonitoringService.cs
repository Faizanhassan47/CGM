namespace CGM.PatientApp.Interfaces;

public interface IForegroundMonitoringService
{
    void StartService(string deviceName = "CGM Sensor");
    void StopService();
    void UpdateReading(string deviceName, double glucoseMgDl, string? trend = null);
}
