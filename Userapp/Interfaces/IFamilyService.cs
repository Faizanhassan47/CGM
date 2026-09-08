using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IFamilyService
{
    Task<Family?> GetAsync();
    Task<Family> CreateAsync(string name);
    Task<Family> JoinAsync(string referralCode);
    Task SetAlertsAsync(int userId, bool enabled);
    Task RemoveAsync(int userId);
    Task LeaveAsync();
    Task<Family> UpdateThresholdsAsync(double low, double high);
}
