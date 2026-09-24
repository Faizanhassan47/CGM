using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface ILocalMeasurementRepository
{
    Task InitializeAsync();
    Task<int> SaveMeasurementAsync(LocalMeasurement measurement);
    Task<List<LocalMeasurement>> GetPendingMeasurementsAsync(int limit = 100);
    Task MarkAsSyncedAsync(IEnumerable<int> ids);
    Task MarkAsFailedAsync(int id, string error);
    Task<List<LocalMeasurement>> GetRecentMeasurementsAsync(int limit = 50);
    Task<List<LocalMeasurement>> GetMeasurementsForDateAsync(DateTime date, int limit = 100);
    Task<int> GetPendingCountAsync();
}
