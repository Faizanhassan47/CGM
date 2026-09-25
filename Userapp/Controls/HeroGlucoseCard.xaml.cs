using System.Windows.Input;

namespace CGM.PatientApp.Controls;

public partial class HeroGlucoseCard : ContentView
{
    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(HeroGlucoseCard), "Glucose Telemetry");

    public static readonly BindableProperty TrendTextProperty =
        BindableProperty.Create(nameof(TrendText), typeof(string), typeof(HeroGlucoseCard), "In Range");

    public static readonly BindableProperty TrendArrowProperty =
        BindableProperty.Create(nameof(TrendArrow), typeof(string), typeof(HeroGlucoseCard), "→");

    public static readonly BindableProperty GlucoseValueProperty =
        BindableProperty.Create(
            nameof(GlucoseValue),
            typeof(string),
            typeof(HeroGlucoseCard),
            "112",
            propertyChanged: (b, _, _) => ((HeroGlucoseCard)b).UpdateCorridorPointer());

    public static readonly BindableProperty GlucoseUnitProperty =
        BindableProperty.Create(nameof(GlucoseUnit), typeof(string), typeof(HeroGlucoseCard), "mg/dL");

    public static readonly BindableProperty DeltaTextProperty =
        BindableProperty.Create(nameof(DeltaText), typeof(string), typeof(HeroGlucoseCard), "Target: 90–130");

    public static readonly BindableProperty LastUpdatedTextProperty =
        BindableProperty.Create(nameof(LastUpdatedText), typeof(string), typeof(HeroGlucoseCard), "Updated just now");

    public static readonly BindableProperty CardColorProperty =
        BindableProperty.Create(
            nameof(CardColor),
            typeof(Color),
            typeof(HeroGlucoseCard),
            Color.FromArgb("#01B4F1"));

    public static readonly BindableProperty VelocityTextProperty =
        BindableProperty.Create(nameof(VelocityText), typeof(string), typeof(HeroGlucoseCard), "Stable velocity (±0.5 mg/dL/min)");

    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(HeroGlucoseCard), null);

    public string TitleText
    {
        get => (string)GetValue(TitleTextProperty);
        set => SetValue(TitleTextProperty, value);
    }

    public string TrendText
    {
        get => (string)GetValue(TrendTextProperty);
        set => SetValue(TrendTextProperty, value);
    }

    public string TrendArrow
    {
        get => (string)GetValue(TrendArrowProperty);
        set => SetValue(TrendArrowProperty, value);
    }

    public string GlucoseValue
    {
        get => (string)GetValue(GlucoseValueProperty);
        set => SetValue(GlucoseValueProperty, value);
    }

    public string GlucoseUnit
    {
        get => (string)GetValue(GlucoseUnitProperty);
        set => SetValue(GlucoseUnitProperty, value);
    }

    public string DeltaText
    {
        get => (string)GetValue(DeltaTextProperty);
        set => SetValue(DeltaTextProperty, value);
    }

    public string LastUpdatedText
    {
        get => (string)GetValue(LastUpdatedTextProperty);
        set => SetValue(LastUpdatedTextProperty, value);
    }

    public Color CardColor
    {
        get => (Color)GetValue(CardColorProperty);
        set => SetValue(CardColorProperty, value);
    }

    public string VelocityText
    {
        get => (string)GetValue(VelocityTextProperty);
        set => SetValue(VelocityTextProperty, value);
    }

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public HeroGlucoseCard()
    {
        InitializeComponent();
        SizeChanged += OnCardSizeChanged;
        Loaded += OnCardLoaded;
    }

    private void OnCardLoaded(object? sender, EventArgs e)
    {
        UpdateCorridorPointer();
    }

    private void OnCardSizeChanged(object? sender, EventArgs e)
    {
        UpdateCorridorPointer();
    }

    private void UpdateCorridorPointer()
    {
        if (CorridorTrack == null || CorridorPointer == null) return;

        double width = CorridorTrack.Width;
        if (width <= 0) return;

        if (double.TryParse(GlucoseValue, out var val))
        {
            // Scale: 40 to 300 mg/dL
            double ratio = Math.Clamp((val - 40.0) / 260.0, 0.0, 1.0);
            double targetX = ratio * (width - CorridorPointer.Width);
            _ = CorridorPointer.TranslateTo(targetX, 0, 300, Easing.CubicOut);
        }
    }
}
