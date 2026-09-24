using Microsoft.Extensions.DependencyInjection;

namespace CGM.PatientApp.Services.Diagnostics;

public static class SafeAsync
{
    public static async void Run(Func<Task> operation, string context)
    {
        try { await operation(); }
        catch (OperationCanceledException) { }
        catch (Exception exception)
        {
            var handler = IPlatformApplication.Current?.Services.GetService<IApplicationErrorHandler>();
            if (handler is not null) await handler.HandleAsync(exception, context);
        }
    }
}
