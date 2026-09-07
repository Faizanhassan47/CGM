using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Cgm;

public class MockSensorService : ISensorService
{
    public Task<SensorInfo> GetCurrentSensorInfoAsync()
    {
        return Task.FromResult(new SensorInfo
        {
            SensorId = "MOCK-123",
            State = CGM.PatientApp.Enums.SensorState.Active,
            ActivationTime = DateTime.UtcNow.AddDays(-1).AddHours(2),
            ExpirationTime = DateTime.UtcNow.AddDays(13)
        });
    }

    public Task<bool> StartSensorSessionAsync(string sensorId)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StopSensorSessionAsync()
    {
        return Task.FromResult(true);
    }
}
