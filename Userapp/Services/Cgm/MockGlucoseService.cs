using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Cgm;

public class MockGlucoseService : IGlucoseService
{
    public event EventHandler<GlucoseMeasurement>? GlucoseReadingReceived;

    public Task<GlucoseSummary> GetDashboardSummaryAsync()
    {
        return Task.FromResult(new GlucoseSummary
        {
            AverageGlucose = 100,
            HighestGlucose = 120,
            LowestGlucose = 80,
            TimeInRangePercentage = 85,
            TimeAboveRangePercentage = 10,
            TimeBelowRangePercentage = 5,
            LatestReading = new GlucoseMeasurement { GlucoseValue = 105 }
        });
    }

    public Task<GlucoseMeasurement?> GetLatestReadingAsync()
    {
        return Task.FromResult<GlucoseMeasurement?>(new GlucoseMeasurement { GlucoseValue = 105 });
    }

    public Task<IReadOnlyList<GlucoseMeasurement>> GetRecentReadingsAsync(TimeSpan timeSpan)
    {
        return Task.FromResult<IReadOnlyList<GlucoseMeasurement>>(new List<GlucoseMeasurement>());
    }

    public Task<IReadOnlyList<GlucoseMeasurement>> GetReadingsForDateAsync(DateTime localDate) =>
        GetRecentReadingsAsync(TimeSpan.FromDays(1));

    public Task<PagedGlucoseReadings> GetReadingsForDateAsync(DateTime localDate, int page, int pageSize) =>
        Task.FromResult(new PagedGlucoseReadings([], page, pageSize, 0, 0));

    public void SimulateReading(GlucoseMeasurement reading)
    {
        GlucoseReadingReceived?.Invoke(this, reading);
    }
}
