namespace CGM.PatientApp.Services.Auth;

public interface ISessionService
{
    Task LogoutAllAsync(CancellationToken ct = default);
}

public sealed class ApiSessionService(HttpClient client) : ISessionService
{
    public async Task LogoutAllAsync(CancellationToken ct = default)
    {
        using var response = await client.PostAsync("api/auth/logout-all", null, ct);
        response.EnsureSuccessStatusCode();
    }
}
