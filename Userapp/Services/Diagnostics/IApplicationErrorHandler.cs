namespace CGM.PatientApp.Services.Diagnostics;
public interface IApplicationErrorHandler
{
    Task HandleAsync(Exception exception,string context,bool showUserMessage=false);
}
public sealed class ApplicationErrorHandler(ICrashReporter reporter) : IApplicationErrorHandler
{
    public async Task HandleAsync(Exception exception,string context,bool showUserMessage=false)
    {
        await reporter.ReportAsync(exception,context);
        if(showUserMessage&&Shell.Current is not null)
            await MainThread.InvokeOnMainThreadAsync(()=>Shell.Current.DisplayAlertAsync("Something went wrong","The operation could not be completed. Please try again.","OK"));
    }
}
