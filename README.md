# GlucoTrack CGM

.NET MAUI patient app plus ASP.NET Core API for account, profile, device, sensor, glucose, alert, and report workflows.

## Run locally

1. Copy `.env.example` to `.env` and replace every placeholder. Use a JWT secret of at least 32 random bytes.
2. Start SQL Server and set `DB_CONNECTION_STRING`.
3. Run the API:

   ```powershell
   dotnet run --project CGM.Api\CGM.Api.csproj
   ```

4. Set `CGM_API_BASE_URL` to the API address and run `Userapp\CGM.PatientApp.csproj` from Visual Studio. Android emulators normally reach the host through `10.0.2.2`.

The app uses real API services by default. Set `CGM_USE_MOCK_SERVICES=true` only for a clearly marked UI demonstration with simulated devices and readings.

## Required production integrations

- Obtain the CGM manufacturer's approved BLE SDK, protocol, UUIDs, commands, validation rules, and test hardware. The production fallback deliberately refuses to fabricate connections or readings until this is supplied.
- Configure Google and Apple OAuth client IDs, redirect URIs, platform entitlements, and server-side identity-token validation before enabling social sign-in.
- Replace development SMTP settings with a transactional email provider and verified sending domain.
- Configure trusted HTTPS certificates, production CORS origins, a managed SQL Server, backups, monitoring, privacy policy, consent flow, retention policy, and regulatory review appropriate to the launch market.
- Add iOS/macOS signing identities, Android signing keys, store metadata, and physical-device acceptance testing.

## Verification

```powershell
dotnet build CGM.Api\CGM.Api.csproj -c Release
dotnet test CGM.PatientApp.Tests\CGM.PatientApp.Tests.csproj -c Release
dotnet build Userapp\CGM.PatientApp.csproj -c Release -f net10.0-windows10.0.19041.0
```

New bundled images and fonts require a full app restart; XAML Hot Reload alone will not load them.
