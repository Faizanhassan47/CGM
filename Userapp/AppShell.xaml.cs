using CGM.PatientApp.Views.Auth;
using CGM.PatientApp.Views.DeviceSetup;
using CGM.PatientApp.Views.Dashboard;
using CGM.PatientApp.Views.Details;
using CGM.PatientApp.Views;
using CGM.PatientApp.Services.Auth;

namespace CGM.PatientApp;

public partial class AppShell : Shell
{
	public AppShell()
	{
		InitializeComponent();

		// Register push routes
		Routing.RegisterRoute("ForgotPasswordPage", typeof(ForgotPasswordPage));
		Routing.RegisterRoute("ResetPasswordPage", typeof(ResetPasswordPage));
		Routing.RegisterRoute("VerifyEmailPage", typeof(VerifyEmailPage));

		// Device Pairing & Onboarding routes
		Routing.RegisterRoute("DevicePreparationPage", typeof(DevicePreparationPage));
		Routing.RegisterRoute("BluetoothPermissionPage", typeof(BluetoothPermissionPage));
		Routing.RegisterRoute("ScanningPage", typeof(ScanningPage));
		Routing.RegisterRoute("DeviceFoundPage", typeof(DeviceFoundPage));
		Routing.RegisterRoute("ConnectingPage", typeof(ConnectingPage));
		Routing.RegisterRoute("ConnectionSuccessPage", typeof(ConnectionSuccessPage));
		Routing.RegisterRoute("AppDetailPage", typeof(AppDetailPage));
		Routing.RegisterRoute("FamilyPage", typeof(FamilyPage));
		Routing.RegisterRoute("login", typeof(LoginPage));

		AuthenticationStateService.Current.UserLoggedOut += OnUserLoggedOut;
	}

	private void OnUserLoggedOut(object? sender, Interfaces.UserLoggedOutEvent e)
	{
		MainThread.BeginInvokeOnMainThread(async () =>
		{
			try
			{
				await GoToAsync("//LoginPage");
				await DisplayAlert("Session Expired", e.Reason, "Sign In");
			}
			catch { }
		});
	}
}
