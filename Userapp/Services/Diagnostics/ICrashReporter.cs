namespace CGM.PatientApp.Services.Diagnostics;

public interface ICrashReporter
{
    Task ReportAsync(Exception exception, string context);
}
