using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Services.Auth;

public sealed class AuthenticationStateService : IAuthenticationStateService
{
    private static AuthenticationStateService? _instance;
    public static AuthenticationStateService Current => _instance ??= new AuthenticationStateService();

    public event EventHandler<UserLoggedOutEvent>? UserLoggedOut;

    public AuthenticationStateService()
    {
        _instance = this;
    }

    public void NotifySessionExpired(string reason = "Your session has expired. Please sign in again.")
    {
        UserLoggedOut?.Invoke(this, new UserLoggedOutEvent(reason));
    }
}
