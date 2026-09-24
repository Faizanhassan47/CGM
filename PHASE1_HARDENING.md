# Phase 1 mobile stability hardening

## Crash diagnostics

The app registers global .NET and Android exception hooks. `LocalCrashReporter` writes JSON-lines diagnostics to `FileSystem.AppDataDirectory/crash-log.jsonl`, including the app version/build, device model, platform version, user ID, UTC timestamp, context, exception, and stack trace. Logging failures never replace the original application error.

`ICrashReporter` is intentionally provider-neutral so a remote provider such as Sentry or Firebase Crashlytics can replace the local implementation without changing pages or view models.

## Safe asynchronous lifecycle

Page lifecycle, navigation, dispatcher, and timer callbacks route task failures through `SafeAsync`, preventing exceptions from escaping framework-required void callbacks. Cancellation during page teardown is treated as expected.

## Dashboard lifecycle

`DashboardViewModel` now has idempotent `Activate` and `Deactivate` methods and implements `IDisposable`. The page subscribes on appearance and unsubscribes on disappearance, preventing retained view models and duplicate glucose/connection callbacks.
