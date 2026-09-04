using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IDeviceService
{
    Task<IReadOnlyList<SupportedCgmDevice>> GetSupportedDevicesAsync();
    Task<CgmDeviceInfo?> GetConfiguredDeviceAsync();
    Task SaveConfiguredDeviceAsync(CgmDeviceInfo device);
    Task RemoveConfiguredDeviceAsync();
}
