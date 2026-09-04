using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class SupportedCgmDevice
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string ModelCode { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public CgmDeviceType DeviceType { get; set; }
    public string IconResource { get; set; } = string.Empty;
    public string PreparationInstructions { get; set; } = string.Empty;
    public bool IsRecommended { get; set; }
}
