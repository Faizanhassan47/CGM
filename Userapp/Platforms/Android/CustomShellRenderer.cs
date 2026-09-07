#if ANDROID
using Android.Content;
using Android.OS;
using Android.Views;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Controls.Handlers.Compatibility;
using Microsoft.Maui.Controls.Platform.Compatibility;

namespace CGM.PatientApp.Platforms.Android;

public class CustomShellRenderer : ShellRenderer
{
    public CustomShellRenderer(Context context) : base(context) { }

    protected override IShellSectionRenderer CreateShellSectionRenderer(ShellSection shellSection)
    {
        return new CustomShellSectionRenderer(this);
    }
}

public class CustomShellSectionRenderer : ShellSectionRenderer
{
    public CustomShellSectionRenderer(IShellContext shellContext) : base(shellContext) { }

    public override global::Android.Views.View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
    {
        try
        {
            return base.OnCreateView(inflater, container, savedInstanceState);
        }
        catch (global::Android.Content.Res.Resources.NotFoundException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomShellSectionRenderer] Caught Resources.NotFoundException: {ex.Message}");
            return new global::Android.Views.View(Context);
        }
        catch (System.Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CustomShellSectionRenderer] Exception: {ex.Message}");
            return new global::Android.Views.View(Context);
        }
    }
}
#endif
