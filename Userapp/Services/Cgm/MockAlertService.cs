using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Cgm;

public class MockAlertService : IAlertService
{
    public event EventHandler<Alert>? AlertTriggered;

    public Task ClearAllAlertsAsync()
    {
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Alert>> GetAlertsAsync(bool unreadOnly = false)
    {
        return Task.FromResult<IReadOnlyList<Alert>>(new List<Alert>());
    }

    public Task<AlertSettings> GetAlertSettingsAsync()
    {
        return Task.FromResult(new AlertSettings());
    }

    public Task MarkAlertAsReadAsync(string alertId)
    {
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
