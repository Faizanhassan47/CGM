using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface ISensorService
{
    Task<SensorInfo> GetCurrentSensorInfoAsync();
    Task<bool> StartSensorSessionAsync(string sensorId);
    Task<bool> StopSensorSessionAsync();
}
