using Microsoft.Extensions.DependencyInjection;

using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp;

public partial class App : Application
{
	private readonly ICrashReporter _crashReporter;

	public App(ICrashReporter crashReporter)
	{
		_crashReporter = crashReporter;
		InitializeComponent();
		AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
		TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
	}

	private void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
	{
		if (args.ExceptionObject is Exception exception)
			SafeAsync.Run(() => _crashReporter.ReportAsync(exception, "AppDomain.UnhandledException"), "CrashReporter.AppDomain");
	}

	private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs args)
	{
		args.SetObserved();
		SafeAsync.Run(() => _crashReporter.ReportAsync(args.Exception, "TaskScheduler.UnobservedTaskException"), "CrashReporter.TaskScheduler");
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new AppShell());
	}
}
