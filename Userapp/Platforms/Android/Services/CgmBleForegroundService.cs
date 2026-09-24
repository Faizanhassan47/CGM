#if ANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;

namespace CGM.PatientApp.Platforms.Android.Services;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeConnectedDevice)]
public class CgmBleForegroundService : Service
{
    public const string ChannelId = "cgm_foreground_monitoring_channel";
    public const string ChannelName = "CGM Continuous Monitoring";
    public const int NotificationId = 9001;

    public const string ActionStart = "CGM_ACTION_START";
    public const string ActionStop = "CGM_ACTION_STOP";
    public const string ActionUpdate = "CGM_ACTION_UPDATE";
    public const string ExtraDeviceName = "EXTRA_DEVICE_NAME";
    public const string ExtraReadingText = "EXTRA_READING_TEXT";

    private static CgmBleForegroundService? _instance;
    private NotificationManager? _notificationManager;
    private static string _currentDeviceName = "CGM Sensor";
    private static string _lastReadingText = "Waiting for initial reading...";

    public static bool IsRunning => _instance != null;

    public override void OnCreate()
    {
        base.OnCreate();
        _instance = this;
        _notificationManager = (NotificationManager?)GetSystemService(NotificationService);
        CreateNotificationChannel();
    }

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        if (intent != null)
        {
            var action = intent.Action;
            if (action == ActionStop)
            {
                StopForeground(StopForegroundFlags.Remove);
                StopSelf();
                return StartCommandResult.NotSticky;
            }

            if (action == ActionUpdate)
            {
                var devName = intent.GetStringExtra(ExtraDeviceName);
                if (!string.IsNullOrWhiteSpace(devName))
                    _currentDeviceName = devName;

                var reading = intent.GetStringExtra(ExtraReadingText);
                if (!string.IsNullOrWhiteSpace(reading))
                    _lastReadingText = reading;

                UpdateNotificationDisplay();
                return StartCommandResult.Sticky;
            }

            var initialDev = intent.GetStringExtra(ExtraDeviceName);
            if (!string.IsNullOrWhiteSpace(initialDev))
                _currentDeviceName = initialDev;
        }

        var notification = BuildNotification();
        if (Build.VERSION.SdkInt >= BuildVersionCodes.Q)
        {
            StartForeground(NotificationId, notification, ForegroundService.TypeConnectedDevice);
        }
        else
        {
            StartForeground(NotificationId, notification);
        }

        return StartCommandResult.Sticky;
    }

    public override IBinder? OnBind(Intent? intent) => null;

    public override void OnDestroy()
    {
        _instance = null;
        base.OnDestroy();
    }

    public static void Start(Context context, string deviceName = "CGM Sensor")
    {
        _currentDeviceName = deviceName;
        var intent = new Intent(context, typeof(CgmBleForegroundService));
        intent.SetAction(ActionStart);
        intent.PutExtra(ExtraDeviceName, deviceName);
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
        {
            context.StartForegroundService(intent);
        }
        else
        {
            context.StartService(intent);
        }
    }

    public static void Stop(Context context)
    {
        var intent = new Intent(context, typeof(CgmBleForegroundService));
        intent.SetAction(ActionStop);
        context.StartService(intent);
    }

    public static void UpdateReading(Context context, string deviceName, string readingText)
    {
        _currentDeviceName = deviceName;
        _lastReadingText = readingText;

        if (!IsRunning)
        {
            Start(context, deviceName);
        }

        var intent = new Intent(context, typeof(CgmBleForegroundService));
        intent.SetAction(ActionUpdate);
        intent.PutExtra(ExtraDeviceName, deviceName);
        intent.PutExtra(ExtraReadingText, readingText);
        context.StartService(intent);
    }

    private void CreateNotificationChannel()
    {
        if (Build.VERSION.SdkInt >= BuildVersionCodes.O && _notificationManager != null)
        {
            var channel = new NotificationChannel(ChannelId, ChannelName, NotificationImportance.Low)
            {
                Description = "Ongoing background Bluetooth connection to your CGM sensor"
            };
            channel.EnableLights(false);
            channel.EnableVibration(false);
            channel.SetShowBadge(false);
            _notificationManager.CreateNotificationChannel(channel);
        }
    }

    private Notification BuildNotification()
    {
        var launchIntent = PackageManager?.GetLaunchIntentForPackage(PackageName ?? string.Empty);
        var pendingIntent = launchIntent != null
            ? PendingIntent.GetActivity(this, 0, launchIntent, PendingIntentFlags.Immutable | PendingIntentFlags.UpdateCurrent)
            : null;

        var shortSummary = $"Connected: {_currentDeviceName} • {_lastReadingText}";
        var bigText = $"Connected:\n{_currentDeviceName}\n\nLast Reading:\n{_lastReadingText}";

        var builder = new NotificationCompat.Builder(this, ChannelId)
            .SetContentTitle("GlucoTrack Monitoring Active")
            .SetContentText(shortSummary)
            .SetStyle(new NotificationCompat.BigTextStyle().BigText(bigText))
            .SetSmallIcon(global::Android.Resource.Drawable.StatNotifySync)
            .SetOngoing(true)
            .SetOnlyAlertOnce(true)
            .SetCategory(NotificationCompat.CategoryService)
            .SetPriority(NotificationCompat.PriorityLow);

        if (pendingIntent != null)
        {
            builder.SetContentIntent(pendingIntent);
        }

        return builder.Build();
    }

    private void UpdateNotificationDisplay()
    {
        if (_notificationManager != null)
        {
            var notification = BuildNotification();
            _notificationManager.Notify(NotificationId, notification);
        }
    }
}
#endif
