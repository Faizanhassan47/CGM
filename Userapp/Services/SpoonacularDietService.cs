using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Services;

public class SpoonacularDietService : ISmartDietService
{
    private readonly HttpClient _httpClient;
    private const string ApiKey = "3c1b1e8a5f594bd0804a55f049f12e39";

    public SpoonacularDietService()
    {
        _httpClient = new HttpClient();
    }

    public Task<DietRecommendation> GetRecommendationAsync(double currentGlucose)
    {
        // Clinical requirements: Do not share health data with 3rd parties without explicit consent.
        // Return clinically reviewed fallback rules instead.
        return Task.FromResult(GetFallbackRecommendation(currentGlucose));
    }

    private DietRecommendation GetFallbackRecommendation(double currentGlucose)
    {
        if (currentGlucose < 70)
        {
            return new DietRecommendation
            {
                Title = "Action Needed: Low",
                Description = "Eat or drink 15g of fast-acting carbs (e.g., half a cup of juice or 3-4 glucose tabs)."
            };
        }
        else if (currentGlucose > 180)
        {
            return new DietRecommendation
            {
                Title = "Above Target",
                Description = "Consider a zero-carb snack like nuts or cheese, or go for a short walk."
            };
        }
        else
        {
            return new DietRecommendation
            {
                Title = "Perfectly in Range",
                Description = "You're doing great! Stick to balanced meals with protein, healthy fats, and fiber."
            };
        }
    }
}
