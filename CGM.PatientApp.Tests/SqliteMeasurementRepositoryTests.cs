using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Database;
using Xunit;

namespace CGM.PatientApp.Tests;

public class SqliteMeasurementRepositoryTests : IDisposable
{
    private readonly string _testDbPath;
    private readonly SqliteMeasurementRepository _repo;

    public SqliteMeasurementRepositoryTests()
    {
        _testDbPath = Path.Combine(Path.GetTempPath(), $"cgm_test_{Guid.NewGuid():N}.db3");
        _repo = new SqliteMeasurementRepository(_testDbPath);
    }

    public void Dispose()
    {
        try
        {
            if (File.Exists(_testDbPath))
                File.Delete(_testDbPath);
        }
        catch { }
    }

    [Fact]
    public async Task SaveMeasurementAsync_InsertsPendingMeasurementSuccessfully()
    {
        await _repo.InitializeAsync();

        var measurement = new LocalMeasurement
        {
            SequenceNumber = 101,
            GlucoseValue = 120.5,
            MeasuredAt = DateTime.UtcNow,
            SyncStatus = "Pending"
        };

        var id = await _repo.SaveMeasurementAsync(measurement);
        Assert.True(id > 0);

        var pending = await _repo.GetPendingMeasurementsAsync(10);
        Assert.Single(pending);
        Assert.Equal(101, pending[0].SequenceNumber);
        Assert.Equal(120.5, pending[0].GlucoseValue);
        Assert.Equal("Pending", pending[0].SyncStatus);
    }

    [Fact]
    public async Task MarkAsSyncedAsync_UpdatesSyncStatusToSynced()
    {
        await _repo.InitializeAsync();

        var measurement = new LocalMeasurement
        {
            SequenceNumber = 202,
            GlucoseValue = 135.0,
            MeasuredAt = DateTime.UtcNow,
            SyncStatus = "Pending"
        };

        var id = await _repo.SaveMeasurementAsync(measurement);
        var pendingCountBefore = await _repo.GetPendingCountAsync();
        Assert.Equal(1, pendingCountBefore);

        await _repo.MarkAsSyncedAsync([id]);

        var pendingCountAfter = await _repo.GetPendingCountAsync();
        Assert.Equal(0, pendingCountAfter);

        var recent = await _repo.GetRecentMeasurementsAsync(5);
        Assert.Single(recent);
        Assert.Equal("Synced", recent[0].SyncStatus);
        Assert.NotNull(recent[0].SyncedAt);
    }
}
