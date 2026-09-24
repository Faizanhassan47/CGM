using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;
using Android.Content;
using Android.Runtime;
using CGM.PatientApp.Services.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace CGM.PatientApp;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
    internal const int GoogleSignInRequestCode = 49001;
    internal static event Action<int, Result, Intent?>? ActivityResultReceived;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        AndroidEnvironment.UnhandledExceptionRaiser -= OnAndroidUnhandledException;
        AndroidEnvironment.UnhandledExceptionRaiser += OnAndroidUnhandledException;

        if (Window != null)
        {
            Window.SetStatusBarColor(Android.Graphics.Color.ParseColor("#01B4F1"));
        }
    }

    private static void OnAndroidUnhandledException(object? sender, RaiseThrowableEventArgs args)
    {
        try
        {
            IPlatformApplication.Current?.Services.GetService<ICrashReporter>()?
                .ReportAsync(args.Exception, "Android.UnhandledException")
                .GetAwaiter().GetResult();
        }
        catch
        {
            // Never replace the original Android exception with a reporting failure.
        }
    }

    protected override void OnActivityResult(int requestCode, Result resultCode, Intent? data)
    {
        base.OnActivityResult(requestCode, resultCode, data);
        ActivityResultReceived?.Invoke(requestCode, resultCode, data);
    }
}
