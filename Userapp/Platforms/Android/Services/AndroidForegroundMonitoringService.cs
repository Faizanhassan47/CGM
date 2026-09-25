#if ANDROID
using Android.App;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Platforms.Android.Services;

public sealed class AndroidForegroundMonitoringService : IForegroundMonitoringService
{
    public void StartService(string deviceName = "CGM Sensor")
    {
        var context = global::Android.App.Application.Context;
        CgmBleForegroundService.Start(context, deviceName);
    }

    public void StopService()
    {
        var context = global::Android.App.Application.Context;
        CgmBleForegroundService.Stop(context);
    }

    public void UpdateReading(string deviceName, double glucoseMgDl, string? trend = null)
    {
        var context = global::Android.App.Application.Context;
        var trendSuffix = !string.IsNullOrWhiteSpace(trend) ? $" ({trend})" : "";
        var readingText = $"{glucoseMgDl:F0} mg/dL{trendSuffix}";
        CgmBleForegroundService.UpdateReading(context, deviceName, readingText);

        if (glucoseMgDl < 90)
        {
            var alertEngine = new AndroidCriticalAlertEngine();
            alertEngine.TriggerHypoEmergencyAlarm(glucoseMgDl);
        }
    }
}
#endif
