namespace CGM.PatientApp.Services.Api;

/// <summary>Single catalog of backend routes used by the mobile application.</summary>
public static class ApiEndpoints
{
    public static class Auth
    {
        public const string Login = "api/auth/login";
        public const string Register = "api/auth/register";
        public const string Refresh = "api/auth/refresh-token";
        public const string Logout = "api/auth/logout";
        public const string LogoutAll = "api/auth/logout-all";
        public const string ForgotPassword = "api/auth/forgot-password";
        public const string VerifyResetCode = "api/auth/verify-reset-code";
        public const string ResetPassword = "api/auth/reset-password";
    }

    public static class Glucose
    {
        public const string Measurement = "api/glucose/measurement";
        public const string BulkSync = "api/glucose/sync-bulk";
        public const string History = "api/glucose/history";
        public const string Summary = "api/glucose/summary";
    }

    public static class Sensors { public const string Active = "api/sensors/active"; public const string Start = "api/sensors/start"; }
    public static class Devices { public const string List = "api/devices"; public const string Register = "api/devices/register"; public static string ById(int id) => $"api/devices/{id}"; }
    public static class Alerts { public const string List = "api/alerts"; public static string MarkRead(long id) => $"api/alerts/{id}/read"; }
    public static class Family { public const string Root = "api/family"; public const string Create = "api/family/create"; public const string Join = "api/family/join"; public const string Leave = "api/family/leave"; }
    public static class Profile { public const string Root = "api/profile"; }
    public static class Notifications { public const string Endpoints = "api/notification-endpoints"; public static string Endpoint(long id) => $"api/notification-endpoints/{id}"; }
}
