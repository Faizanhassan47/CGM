using CommunityToolkit.Maui;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
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
using CGM.PatientApp.Views.Auth;
using CGM.PatientApp.Views.Dashboard;
using CGM.PatientApp.Views.DeviceSetup;
using CGM.PatientApp.Views.Details;

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
		builder.Services.AddSingleton<MockAuthService>();
		// Initialize environment configuration from .env file or bundled asset
		EnvConfig.Initialize();

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

		builder.Services.AddSingleton(new HttpClient
		{
			BaseAddress = new Uri(apiBaseUrl),
			Timeout = TimeSpan.FromSeconds(15)
		});
		builder.Services.AddSingleton<ApiAuthService>();
		builder.Services.AddSingleton<ApiProfileService>();
		builder.Services.AddSingleton<ApiDeviceService>();
		builder.Services.AddSingleton<UnavailableSocialAuthService>();
		builder.Services.AddSingleton<IAuthService>(sp => useMockServices
			? sp.GetRequiredService<MockAuthService>()
			: sp.GetRequiredService<ApiAuthService>());
		builder.Services.AddSingleton<ISocialAuthService>(sp => useMockServices
			? sp.GetRequiredService<MockAuthService>()
			: sp.GetRequiredService<UnavailableSocialAuthService>());
		builder.Services.AddSingleton<IProfileService>(sp => useMockServices
			? sp.GetRequiredService<MockAuthService>()
			: sp.GetRequiredService<ApiProfileService>());

		builder.Services.AddSingleton<ILocalCacheService>(sp => new LocalCacheService());
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
		builder.Services.AddSingleton<ICgmProtocolParser>(sp => new CgmProtocolParser());
		builder.Services.AddSingleton<CgmCommandBuilder>();
		builder.Services.AddSingleton<ICgmDeviceService, CgmDeviceService>();
		builder.Services.AddSingleton<CGM.PatientApp.Services.Reports.IPdfReportService, CGM.PatientApp.Services.Reports.PdfReportService>();

		// Register ViewModels
		builder.Services.AddTransient<SplashViewModel>();
		builder.Services.AddTransient<LoginViewModel>();
		builder.Services.AddTransient<SignUpViewModel>();
		builder.Services.AddTransient<ForgotPasswordViewModel>();
		builder.Services.AddTransient<ResetPasswordViewModel>();
		builder.Services.AddTransient<CompleteProfileViewModel>();
		builder.Services.AddTransient<DeviceSelectionViewModel>();
		builder.Services.AddTransient<DevicePreparationViewModel>();
		builder.Services.AddTransient<BluetoothPermissionViewModel>();
		builder.Services.AddTransient<ScanningViewModel>();
		builder.Services.AddTransient<DeviceFoundViewModel>();
		builder.Services.AddTransient<ConnectingViewModel>();
		builder.Services.AddTransient<ConnectionSuccessViewModel>();
		builder.Services.AddTransient<DashboardViewModel>();

		// Register Pages
		builder.Services.AddTransient<SplashPage>();
		builder.Services.AddTransient<LoginPage>();
		builder.Services.AddTransient<SignUpPage>();
		builder.Services.AddTransient<ForgotPasswordPage>();
		builder.Services.AddTransient<ResetPasswordPage>();
		builder.Services.AddTransient<CompleteProfilePage>();
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
