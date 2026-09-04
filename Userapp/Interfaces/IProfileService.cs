using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IProfileService
{
    Task<PatientProfile?> GetProfileAsync();
    Task<ApiResponse<PatientProfile>> SaveProfileAsync(PatientProfile profile);
    Task<ApiResponse<bool>> CompleteProfileAsync(CompleteProfileRequest request);
}
