using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace CGM.PatientApp.Services.Diagnostics;

public sealed class LocalCrashReporter(ILogger<LocalCrashReporter> logger) : ICrashReporter
{
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    public async Task ReportAsync(Exception exception, string context)
    {
        logger.LogError(exception, "Mobile failure in {Context}", context);
        try
        {
            var entry = JsonSerializer.Serialize(new
            {
                timestamp = DateTimeOffset.UtcNow,
                context,
                appVersion = AppInfo.Current.VersionString,
                appBuild = AppInfo.Current.BuildString,
                deviceModel = DeviceInfo.Current.Model,
                platform = DeviceInfo.Current.Platform.ToString(),
                platformVersion = DeviceInfo.Current.VersionString,
                userId = Preferences.Default.Get("cgm_user_id", "anonymous"),
                exception = exception.ToString()
            });
            var path = Path.Combine(FileSystem.AppDataDirectory, "crash-log.jsonl");
            await FileLock.WaitAsync();
            try { await File.AppendAllTextAsync(path, entry + Environment.NewLine); }
            finally { FileLock.Release(); }
        }
        catch (Exception loggingException)
        {
            logger.LogError(loggingException, "Failed to persist mobile crash diagnostics");
        }
    }
}
