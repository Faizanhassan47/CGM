namespace CGM.PatientApp.Interfaces;

public record UserLoggedOutEvent(string Reason);

public interface IAuthenticationStateService
{
    event EventHandler<UserLoggedOutEvent>? UserLoggedOut;
    void NotifySessionExpired(string reason = "Your session has expired. Please sign in again.");
}
