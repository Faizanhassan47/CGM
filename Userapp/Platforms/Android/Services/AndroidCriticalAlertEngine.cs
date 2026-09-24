#if ANDROID
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using AndroidX.Core.App;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Platforms.Android.Services;

public sealed class AndroidCriticalAlertEngine : ICriticalAlertEngine
{
    public const string ChannelId = "cgm_hypo_critical_alarm_channel";
    public const string ChannelName = "Emergency Hypoglycemia Alarm";
    public const int NotificationId = 9999;
    private static Ringtone? _activeRingtone;
    private static PowerManager.WakeLock? _wakeLock;

    public AndroidCriticalAlertEngine()
    {
        CreateAlarmChannel();
    }

    private void CreateAlarmChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            var context = global::Android.App.Application.Context;
            var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            if (notificationManager == null) return;

            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Max)
            {
                Description = "Critical life-safety alarms for severe hypoglycemia (<55 mg/dL) that wake the user"
            };

            var alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm)
                ?? RingtoneManager.GetDefaultUri(RingtoneType.Notification);

            var audioAttributes = new AudioAttributes.Builder()
                ?.SetUsage(AudioUsageKind.Alarm)
                ?.SetContentType(AudioContentType.Sonification)
                ?.Build();

            if (audioAttributes != null)
            {
                channel.SetSound(alarmUri, audioAttributes);
            }

            channel.EnableLights(true);
            channel.LightColor = global::Android.Graphics.Color.ParseColor("#583295");
            channel.EnableVibration(true);
            channel.SetVibrationPattern(new long[] { 0, 1000, 500, 1000, 500, 1000, 1000 });
            channel.SetBypassDnd(true);
            channel.LockscreenVisibility = NotificationVisibility.Public;

            notificationManager.CreateNotificationChannel(channel);
        }
    }

    public void TriggerHypoEmergencyAlarm(double glucoseValueMgDl)
    {
        Context context = global::Android.App.Application.Context;
        var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);

        // 1. Wake the CPU and screen so sleeping user can see the alert immediately
        try
        {
            var powerManager = (PowerManager?)context.GetSystemService(Context.PowerService);
            if (powerManager != null)
            {
                if (_wakeLock?.IsHeld == true)
                {
                    _wakeLock.Release();
                }
#pragma warning disable CS0618
                _wakeLock = powerManager.NewWakeLock(
                    WakeLockFlags.Full | WakeLockFlags.AcquireCausesWakeup | WakeLockFlags.OnAfterRelease,
                    "CGM:HypoEmergencyWakeLock");
#pragma warning restore CS0618
                _wakeLock.Acquire(30000); // 30 seconds max
            }
        }
        catch { }

        // 2. Play continuous loud alarm audio using Alarm stream to bypass silent/vibrate mode
        try
        {
            var alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm)
                ?? RingtoneManager.GetDefaultUri(RingtoneType.Notification);
            if (alarmUri != null)
            {
                _activeRingtone?.Stop();
                _activeRingtone = RingtoneManager.GetRingtone(context, alarmUri);
                if (Build.VERSION.SdkInt >= BuildVersionCodes.P && _activeRingtone != null)
                {
                    _activeRingtone.AudioAttributes = new AudioAttributes.Builder()
                        ?.SetUsage(AudioUsageKind.Alarm)
                        ?.SetContentType(AudioContentType.Sonification)
                        ?.Build();
                }
                _activeRingtone?.Play();
            }
        }
        catch { }

        // 3. Build and fire full-screen heads-up notification with highest priority
        var launchIntent = context.PackageManager?.GetLaunchIntentForPackage(context.PackageName ?? string.Empty);
        var pendingIntent = launchIntent != null
            ? PendingIntent.GetActivity(context, 0, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
            : null;

        var builder = new NotificationCompat.Builder(context, ChannelId)
            .SetContentTitle("🚨 URGENT HYPOGLYCEMIA ALERT")
            .SetContentText($"CRITICAL: Glucose is {glucoseValueMgDl:F0} mg/dL! Treat immediately with 15g fast-acting sugar.")
            .SetSmallIcon(global::Android.Resource.Drawable.StatNotifyError)
            .SetPriority(NotificationCompat.PriorityMax)
            .SetCategory(NotificationCompat.CategoryAlarm)
            .SetVisibility(NotificationCompat.VisibilityPublic)
            .SetOngoing(true)
            .SetAutoCancel(false);

        if (pendingIntent != null)
        {
            builder.SetContentIntent(pendingIntent);
            builder.SetFullScreenIntent(pendingIntent, true);
        }

        notificationManager?.Notify(NotificationId, builder.Build());
    }

    public void DismissAlarm()
    {
        try
        {
            _activeRingtone?.Stop();
            _activeRingtone = null;
        }
        catch { }

        try
        {
            if (_wakeLock?.IsHeld == true)
            {
                _wakeLock.Release();
                _wakeLock = null;
            }
        }
        catch { }

        try
        {
            var context = global::Android.App.Application.Context;
            var notificationManager = (NotificationManager?)context.GetSystemService(Context.NotificationService);
            notificationManager?.Cancel(NotificationId);
        }
        catch { }
    }
}
#endif
