# Frontend API layer

All backend communication belongs under `Services`; ViewModels must depend on interfaces and must not create `HttpClient` or contain route strings.

- `ApiEndpoints.cs` is the single backend-route catalog.
- `Auth/ApiAuthService.cs` handles identity and sessions.
- `Cgm/ApiGlucoseService.cs`, `ApiSensorService.cs`, `ApiAlertService.cs`, and `ApiNotificationEndpointService.cs` handle CGM APIs.
- `Device/ApiDeviceService.cs`, `Family/ApiFamilyService.cs`, and `Auth/ApiProfileService.cs` handle their domain APIs.
- `Auth/AuthenticatedHttpHandler.cs` attaches access tokens and performs one safe refresh/retry.

Keep request/response DTO mapping in these services. UI and ViewModels should only consume `IAuthService`, `IGlucoseService`, `ISensorService`, `IDeviceService`, `IAlertService`, and related interfaces.
