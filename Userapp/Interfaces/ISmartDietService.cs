using System.Threading.Tasks;

namespace CGM.PatientApp.Interfaces;

public class DietRecommendation
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ImageUrl { get; set; } = string.Empty;
}

public interface ISmartDietService
{
    Task<DietRecommendation> GetRecommendationAsync(double currentGlucose);
}
