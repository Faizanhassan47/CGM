using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IHistoryService
{
    Task<IReadOnlyList<GlucoseMeasurement>> GetHistoryByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<GlucoseMeasurement?> GetMeasurementBySequenceNumberAsync(ushort sequenceNumber);
    Task SaveMeasurementsAsync(IEnumerable<GlucoseMeasurement> measurements);
    Task<ushort> GetLastSavedSequenceNumberAsync();
}
