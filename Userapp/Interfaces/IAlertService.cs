using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IAlertService
{
    event EventHandler<Alert>? AlertTriggered;
    Task<IReadOnlyList<Alert>> GetAlertsAsync(bool unreadOnly = false);
    Task MarkAlertAsReadAsync(string alertId);
    Task ClearAllAlertsAsync();
    Task<AlertSettings> GetAlertSettingsAsync();
    Task<bool> UpdateAlertSettingsAsync(AlertSettings settings);
}
