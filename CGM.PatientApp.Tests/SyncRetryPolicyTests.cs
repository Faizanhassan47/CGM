using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Sync;

namespace CGM.PatientApp.Tests;

public sealed class SyncRetryPolicyTests
{
    private static SyncQueueItem Item(int retries = 0) => new(Guid.NewGuid(), "GlucoseMeasurement",
        new CgmRawMeasurement(), retries, "Pending", DateTime.UtcNow, null, null);

    [Fact]
    public void Failure_remains_pending_before_retry_limit()
    {
        var result = SyncRetryPolicy.FailedAttempt(Item(), "network", DateTime.UtcNow);
        Assert.Equal(1, result.RetryCount);
        Assert.Equal("Pending", result.Status);
    }

    [Fact]
    public void Third_failure_moves_item_to_failed_and_bounds_diagnostic_text()
    {
        var result = SyncRetryPolicy.FailedAttempt(Item(2), new string('x', 700), DateTime.UtcNow);
        Assert.Equal(3, result.RetryCount);
        Assert.Equal("Failed", result.Status);
        Assert.Equal(500, result.ErrorMessage!.Length);
    }
}
