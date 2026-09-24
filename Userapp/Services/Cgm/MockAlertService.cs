using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Cgm;

public class MockAlertService : IAlertService
{
    private readonly List<Alert> _alerts =
    [
        new Alert
        {
            Id = "mock-low-glucose",
            Category = CGM.PatientApp.Enums.AlertCategory.LowGlucose,
            Title = "Low glucose",
            Message = "Glucose dropped below the configured low threshold.",
            Timestamp = DateTime.UtcNow.AddMinutes(-25),
            GlucoseValue = 68,
            Unit = CGM.PatientApp.Enums.GlucoseUnit.MgDl,
            IsRead = false,
            IsCritical = true
        },
        new Alert
        {
            Id = "mock-sensor-status",
            Category = CGM.PatientApp.Enums.AlertCategory.SensorEnding,
            Title = "Sensor status",
            Message = "Your sensor is connected and sending glucose readings.",
            Timestamp = DateTime.UtcNow.AddHours(-2),
            IsRead = true,
            IsCritical = false
        }
    ];

    public event EventHandler<Alert>? AlertTriggered;

    public Task ClearAllAlertsAsync()
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Alert>> GetAlertsAsync(bool unreadOnly = false)
    {
        IReadOnlyList<Alert> result = _alerts
            .Where(alert => !unreadOnly || !alert.IsRead)
            .OrderByDescending(alert => alert.Timestamp)
            .ToList();
        return Task.FromResult(result);
    }

    public Task<AlertSettings> GetAlertSettingsAsync()
    {
        return Task.FromResult(new AlertSettings());
    }

    public Task MarkAlertAsReadAsync(string alertId)
    {
        var alert = _alerts.FirstOrDefault(item => item.Id == alertId);
        if (alert != null)
            alert.IsRead = true;
        return Task.CompletedTask;
    }

    public Task<bool> UpdateAlertSettingsAsync(AlertSettings settings)
    {
        return Task.FromResult(true);
    }

    public void SimulateAlert(Alert alert)
    {
        AlertTriggered?.Invoke(this, alert);
    }
}
