using CGM.PatientApp.Interfaces;
using Plugin.LocalNotification;

namespace CGM.PatientApp.Services;

public sealed class CrossPlatformCriticalAlertEngine : ICriticalAlertEngine
{
    public void TriggerHypoEmergencyAlarm(double glucoseValueMgDl)
    {
        // TODO: Update to the new Plugin.LocalNotification v14 syntax
        // var request = new Plugin.LocalNotification.NotificationRequest
        // {
        //     NotificationId = 9999,
        //     Title = "🚨 URGENT HYPOGLYCEMIA ALERT",
        //     Description = $"CRITICAL: Glucose is {glucoseValueMgDl:F0} mg/dL! Treat immediately with 15g fast-acting sugar.",
        //     ReturningData = "CriticalHypo"
        // };
        // LocalNotificationCenter.Current.Show(request);
    }

    public void DismissAlarm()
    {
        // LocalNotificationCenter.Current.Cancel(9999);
    }
}
