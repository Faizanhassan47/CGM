using CGM.PatientApp.Enums;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IGlucoseService
{
    event EventHandler<GlucoseMeasurement>? GlucoseReadingReceived;
    Task<GlucoseMeasurement?> GetLatestReadingAsync();
    Task<GlucoseSummary> GetDashboardSummaryAsync();
    Task<IReadOnlyList<GlucoseMeasurement>> GetRecentReadingsAsync(TimeSpan timeSpan);
    Task<IReadOnlyList<GlucoseMeasurement>> GetReadingsForDateAsync(DateTime localDate);
    Task<PagedGlucoseReadings> GetReadingsForDateAsync(DateTime localDate, int page, int pageSize);
}
