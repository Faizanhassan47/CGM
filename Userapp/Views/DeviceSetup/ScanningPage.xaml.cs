using CGM.PatientApp.ViewModels.DeviceSetup;
using CGM.PatientApp.Services.Diagnostics;

namespace CGM.PatientApp.Views.DeviceSetup;

public partial class ScanningPage : ContentPage
{
    private readonly ScanningViewModel _viewModel;
    private CancellationTokenSource? _radarCts;

    public ScanningPage() : this(
        (IPlatformApplication.Current?.Services ?? Application.Current?.Handler?.MauiContext?.Services)?.GetService<ScanningViewModel>() 
        ?? throw new InvalidOperationException("ScanningViewModel not found"))
    {
    }

    public ScanningPage(ScanningViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = _viewModel = viewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SafeAsync.Run(_viewModel.StartScanAsync, "ScanningPage.OnAppearing");
        StartRadarAnimation();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        StopRadarAnimation();
        SafeAsync.Run(_viewModel.StopScanAsync, "ScanningPage.OnDisappearing");
    }

    private void StartRadarAnimation()
    {
        StopRadarAnimation();
        _radarCts = new CancellationTokenSource();
        var token = _radarCts.Token;

        SafeAsync.Run(async () =>
        {
            while (!token.IsCancellationRequested)
            {
                var t1 = AnimateRingAsync(RadarRing1, 0, token);
                var t2 = AnimateRingAsync(RadarRing2, 350, token);
                var t3 = AnimateRingAsync(RadarRing3, 700, token);
                await Task.WhenAll(t1, t2, t3);
                if (token.IsCancellationRequested) break;
                await Task.Delay(200, token);
            }
        }, "ScanningPage.RadarAnimation");
    }

    private static async Task AnimateRingAsync(VisualElement? ring, int delayMs, CancellationToken token)
    {
        if (ring == null) return;
        try
        {
            if (delayMs > 0) await Task.Delay(delayMs, token);
            if (token.IsCancellationRequested) return;

            ring.Scale = 0.8;
            ring.Opacity = 0.75;
            await Task.WhenAll(
                ring.ScaleTo(1.35, 1700, Easing.CubicOut),
                ring.FadeTo(0.0, 1700, Easing.CubicOut)
            );
            ring.Scale = 0.8;
            ring.Opacity = 0.0;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ScanningPage] Ring animation error: {ex.Message}");
        }
    }

    private void StopRadarAnimation()
    {
        _radarCts?.Cancel();
        _radarCts?.Dispose();
        _radarCts = null;
    }
}
