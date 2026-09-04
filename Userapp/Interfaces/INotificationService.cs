using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface INotificationService
{
    Task RequestPermissionAsync();
    Task ShowLocalAlertNotificationAsync(Alert alert);
    Task RegisterDevicePushTokenAsync(string token);
}
