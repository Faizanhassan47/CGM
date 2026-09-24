using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using SkiaSharp.Views.Maui.Controls.Hosting;
using LiveChartsCore.SkiaSharpView.Maui;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Services.Auth;
using CGM.PatientApp.Services.Ble;
using CGM.PatientApp.Services.Cache;
using CGM.PatientApp.Services.Cgm;
using CGM.PatientApp.Services.Config;
using CGM.PatientApp.Services.Device;
using CGM.PatientApp.ViewModels.Auth;
using CGM.PatientApp.ViewModels.Dashboard;
using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.ViewModels;
using CGM.PatientApp.Views.Auth;
using CGM.PatientApp.Views.Dashboard;
using CGM.PatientApp.Views.DeviceSetup;
using CGM.PatientApp.Views.Details;
using CGM.PatientApp.Services.Family;
using CGM.PatientApp.Views;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.UseMauiCommunityToolkit()
			.UseSkiaSharp()
			.UseLiveCharts()
			.UseLocalNotification()
			.ConfigureMauiHandlers(handlers =>
			{
#if ANDROID
				handlers.AddHandler(typeof(Shell), typeof(CGM.PatientApp.Platforms.Android.CustomShellRenderer));
#endif
			})
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("FontAwesomeSolid.otf", "FontAwesomeSolid");
				fonts.AddFont("FontAwesomeRegular.otf", "FontAwesomeRegular");
				fonts.AddFont("FontAwesomeBrands.otf", "FontAwesomeBrands");
				fonts.AddFont("Manrope.ttf", "Manrope");
				fonts.AddFont("Manrope-Regular.ttf", "ManropeRegular");
				fonts.AddFont("Manrope-SemiBold.ttf", "ManropeSemiBold");
				fonts.AddFont("Manrope-Bold.ttf", "ManropeBold");
			});

#if DEBUG
		builder.Logging.AddDebug();
#endif

		// Register Services
		builder.Services.AddSingleton<ICrashReporter, LocalCrashReporter>();
		builder.Services.AddSingleton<IApplicationErrorHandler, ApplicationErrorHandler>();
		builder.Services.AddSingleton<MockAuthService>();
		// Initialize environment configuration from .env file or bundled asset
		EnvConfig.Initialize();

#if DEBUG
		var useMockServices = EnvConfig.GetBool("CGM_USE_MOCK_SERVICES",
			Preferences.Default.Get("cgm_use_mock_services", false));

		var defaultUrl = 
#if ANDROID
			// Physical USB devices use `adb reverse tcp:5232 tcp:5232` during development.
			// Emulator users can set CGM_API_BASE_URL=http://10.0.2.2:5232/.
			"http://127.0.0.1:5232/";
#else
			"http://localhost:5232/";
#endif
		var prefUrl = Preferences.Default.Get("cgm_api_base_url", string.Empty);
		var apiBaseUrl = !string.IsNullOrWhiteSpace(prefUrl)
			? prefUrl
			: EnvConfig.Get("CGM_API_BASE_URL", defaultUrl);
#else
        var useMockServices = false;
        var apiBaseUrl = EnvConfig.Get("CGM_API_BASE_URL", "https://api.glucotrack.com/");
#endif
		
		var parsedBaseAddress = new Uri(apiBaseUrl);
		var environment = EnvConfig.Get("CGM_ENVIRONMENT", "Development");
		if (environment.Equals("Production", StringComparison.OrdinalIgnoreCase) && parsedBaseAddress.Scheme != Uri.UriSchemeHttps)
			throw new InvalidOperationException("Production API URL must use HTTPS.");
#if !DEBUG
        if (parsedBaseAddress.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("Release builds must use HTTPS.");
#endif
		builder.Services.AddSingleton(new HttpClient(new AuthenticatedHttpHandler(parsedBaseAddress))
		{
			BaseAddress = parsedBaseAddress,
			Timeout = TimeSpan.FromSeconds(15)
		});
		builder.Services.AddSingleton<IAuthenticationStateService>(CGM.PatientApp.Services.Auth.AuthenticationStateService.Current);
		builder.Services.AddSingleton<ApiAuthService>();
		builder.Services.AddSingleton<ISessionService, ApiSessionService>();
		builder.Services.AddSingleton<ApiProfileService>();
		builder.Services.AddSingleton<ApiDeviceService>();
		builder.Services.AddSingleton<UnavailableSocialAuthService>();
#if ANDROID
		builder.Services.AddSingleton<Platforms.Android.Services.GoogleSocialAuthService>();
#endif
		builder.Services.AddSingleton<IAuthService>(sp => useMockServices
			? sp.GetRequiredService<MockAuthService>()
			: sp.GetRequiredService<ApiAuthService>());
		builder.Services.AddSingleton<ISocialAuthService>(sp => useMockServices
			? sp.GetRequiredService<MockAuthService>()
			:
#if ANDROID
			sp.GetRequiredService<Platforms.Android.Services.GoogleSocialAuthService>());
#else
			sp.GetRequiredService<UnavailableSocialAuthService>());
#endif
		builder.Services.AddSingleton<IProfileService>(sp => useMockServices
			? sp.GetRequiredService<MockAuthService>()
			: sp.GetRequiredService<ApiProfileService>());

		builder.Services.AddSingleton<ILocalCacheService>(sp => new LocalCacheService());
		builder.Services.AddSingleton<ILocalMeasurementRepository, CGM.PatientApp.Services.Database.SqliteMeasurementRepository>();
		builder.Services.AddSingleton<IDeviceService>(sp => useMockServices
			? new MockDeviceService()
			: sp.GetRequiredService<ApiDeviceService>());
		builder.Services.AddSingleton<IBleService>(sp =>
#if ANDROID
			useMockServices && !Preferences.Default.Get("cgm_use_real_ble", true)
				? new MockBleService()
				: new AndroidBleService());
#else
			useMockServices
				? new MockBleService()
				: new UnavailableBleService());
#endif
		builder.Services.AddSingleton<ApiSensorService>();
		builder.Services.AddSingleton<MockSensorService>();
		builder.Services.AddSingleton<ISensorService>(sp => useMockServices
			? sp.GetRequiredService<MockSensorService>()
			: sp.GetRequiredService<ApiSensorService>());

		builder.Services.AddSingleton<ApiGlucoseService>();
		builder.Services.AddSingleton<MockGlucoseService>();
		builder.Services.AddSingleton<IGlucoseService>(sp => useMockServices
			? sp.GetRequiredService<MockGlucoseService>()
			: sp.GetRequiredService<ApiGlucoseService>());
		builder.Services.AddSingleton<RealtimeGlucoseService>();

		builder.Services.AddSingleton<ApiAlertService>();
		builder.Services.AddSingleton<INotificationEndpointService, ApiNotificationEndpointService>();
		builder.Services.AddSingleton<MockAlertService>();
		builder.Services.AddSingleton<IAlertService>(sp => useMockServices
			? sp.GetRequiredService<MockAlertService>()
			: sp.GetRequiredService<ApiAlertService>());

#if ANDROID
		builder.Services.AddSingleton<IForegroundMonitoringService, Platforms.Android.Services.AndroidForegroundMonitoringService>();
		builder.Services.AddSingleton<ICriticalAlertEngine, Platforms.Android.Services.AndroidCriticalAlertEngine>();
#else
		builder.Services.AddSingleton<IForegroundMonitoringService, CGM.PatientApp.Services.NoOpForegroundMonitoringService>();
		builder.Services.AddSingleton<ICriticalAlertEngine, CGM.PatientApp.Services.CrossPlatformCriticalAlertEngine>();
#endif
        builder.Services.AddSingleton<CGM.PatientApp.Services.Sync.ISyncService, CGM.PatientApp.Services.Sync.SyncService>();

		builder.Services.AddSingleton<ICgmProtocolParser>(sp => new CgmProtocolParser());
		builder.Services.AddSingleton<CgmCommandBuilder>();
		builder.Services.AddSingleton<ICgmDeviceService, CgmDeviceService>();
		builder.Services.AddSingleton<CGM.PatientApp.Services.Reports.IPdfReportService, CGM.PatientApp.Services.Reports.PdfReportService>();
		builder.Services.AddSingleton<IReportService, CGM.PatientApp.Services.Reports.ApiReportService>();
		builder.Services.AddSingleton<IFamilyService, ApiFamilyService>();
		builder.Services.AddSingleton<ISmartDietService, CGM.PatientApp.Services.SpoonacularDietService>();

		// Register ViewModels
		builder.Services.AddTransient<SplashViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<SignUpViewModel>();
		builder.Services.AddTransient<ForgotPasswordViewModel>();
		builder.Services.AddTransient<ResetPasswordViewModel>();
		builder.Services.AddTransient<VerifyEmailViewModel>();
		builder.Services.AddTransient<CompleteProfileViewModel>();
		builder.Services.AddTransient<DeviceSelectionViewModel>();
		builder.Services.AddTransient<DevicePreparationViewModel>();
		builder.Services.AddTransient<BluetoothPermissionViewModel>();
		builder.Services.AddTransient<ScanningViewModel>();
		builder.Services.AddTransient<DeviceFoundViewModel>();
		builder.Services.AddTransient<ConnectingViewModel>();
		builder.Services.AddTransient<ConnectionSuccessViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();
		builder.Services.AddTransient<FamilyViewModel>();

		// Register Pages
		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SignUpPage>();
		builder.Services.AddTransient<ForgotPasswordPage>();
		builder.Services.AddTransient<ResetPasswordPage>();
		builder.Services.AddTransient<VerifyEmailPage>();
		builder.Services.AddTransient<CompleteProfilePage>();
		builder.Services.AddTransient<FamilyPage>();
		builder.Services.AddTransient<DeviceSelectionPage>();
		builder.Services.AddTransient<DevicePreparationPage>();
		builder.Services.AddTransient<BluetoothPermissionPage>();
		builder.Services.AddTransient<ScanningPage>();
		builder.Services.AddTransient<DeviceFoundPage>();
		builder.Services.AddTransient<ConnectingPage>();
		builder.Services.AddTransient<ConnectionSuccessPage>();
		builder.Services.AddTransient<DashboardPage>();
		builder.Services.AddTransient<AppDetailPage>();

		return builder.Build();
	}
}
