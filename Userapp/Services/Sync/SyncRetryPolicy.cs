namespace CGM.PatientApp.Services.Sync;

using CGM.PatientApp.Models;

public sealed record SyncQueueItem(
    Guid Id,
    string EntityType,
    CgmRawMeasurement Measurement,
    int RetryCount,
    string Status,
    DateTime CreatedAt,
    DateTime? LastAttempt,
    string? ErrorMessage);

public static class SyncRetryPolicy
{
    public const int MaxRetries = 3;

    public static SyncQueueItem FailedAttempt(SyncQueueItem item, string error, DateTime attemptedAt)
    {
        var retries = item.RetryCount + 1;
        return item with
        {
            RetryCount = retries,
            Status = retries >= MaxRetries ? "Failed" : "Pending",
            LastAttempt = attemptedAt,
            ErrorMessage = error.Length > 500 ? error[..500] : error
        };
    }
}
