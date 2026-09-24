using Android.App;
using Android.Content;
using Android.Gms.Auth.Api.SignIn;
using Android.Gms.Common.Apis;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Platforms.Android.Services;

public sealed class GoogleSocialAuthService : ISocialAuthService
{
    // Firebase Web OAuth client. RequestIdToken requires the web client, not the Android client.
    private const string WebClientId = "203000957883-r5lchnrp5e9mq03dt95fb0qg64io5hrg.apps.googleusercontent.com";

    public async Task<string?> AuthenticateWithGoogleAsync()
    {
        var activity = Platform.CurrentActivity
            ?? throw new InvalidOperationException("Google sign-in requires an active Android activity.");

        var options = new GoogleSignInOptions.Builder(GoogleSignInOptions.DefaultSignIn)
            .RequestEmail()
            .RequestIdToken(WebClientId)
            .Build();
        var client = GoogleSignIn.GetClient(activity, options);
        await client.SignOutAsync();

        var completion = new TaskCompletionSource<string?>(TaskCreationOptions.RunContinuationsAsynchronously);
        void HandleResult(int requestCode, Result resultCode, Intent? data)
        {
            if (requestCode != MainActivity.GoogleSignInRequestCode)
                return;

            MainActivity.ActivityResultReceived -= HandleResult;
            if (resultCode != Result.Ok || data is null)
            {
                completion.TrySetResult(null);
                return;
            }

            try
            {
                var task = GoogleSignIn.GetSignedInAccountFromIntent(data);
                if (!task.IsSuccessful)
                    throw new InvalidOperationException(task.Exception is null
                        ? "Google sign-in did not complete successfully."
                        : task.Exception.ToString());
                var account = task.Result as GoogleSignInAccount;
                completion.TrySetResult(account?.IdToken);
            }
            catch (Exception ex)
            {
                completion.TrySetException(ex);
            }
        }

        MainActivity.ActivityResultReceived += HandleResult;
        try
        {
            activity.StartActivityForResult(client.SignInIntent, MainActivity.GoogleSignInRequestCode);
            return await completion.Task;
        }
        finally
        {
            MainActivity.ActivityResultReceived -= HandleResult;
        }
    }

    public Task<AppleAuthRequest?> AuthenticateWithAppleAsync() =>
        Task.FromResult<AppleAuthRequest?>(null);
}
