using System.Windows.Input;

namespace CGM.PatientApp.Controls;

public partial class HeroGlucoseCard : ContentView
{
    public static readonly BindableProperty TitleTextProperty =
        BindableProperty.Create(nameof(TitleText), typeof(string), typeof(HeroGlucoseCard), "Glucose Now");

    public static readonly BindableProperty TrendTextProperty =
        BindableProperty.Create(nameof(TrendText), typeof(string), typeof(HeroGlucoseCard), "Stable");

    public static readonly BindableProperty TrendArrowProperty =
        BindableProperty.Create(nameof(TrendArrow), typeof(string), typeof(HeroGlucoseCard), "→");

    public static readonly BindableProperty GlucoseValueProperty =
        BindableProperty.Create(nameof(GlucoseValue), typeof(string), typeof(HeroGlucoseCard), "112");

    public static readonly BindableProperty GlucoseUnitProperty =
        BindableProperty.Create(nameof(GlucoseUnit), typeof(string), typeof(HeroGlucoseCard), "mg/dL");

    public static readonly BindableProperty LastUpdatedTextProperty =
        BindableProperty.Create(nameof(LastUpdatedText), typeof(string), typeof(HeroGlucoseCard), "Updated just now");

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

    public string LastUpdatedText
    {
        get => (string)GetValue(LastUpdatedTextProperty);
        set => SetValue(LastUpdatedTextProperty, value);
    }

    public ICommand? TapCommand
    {
        get => (ICommand?)GetValue(TapCommandProperty);
        set => SetValue(TapCommandProperty, value);
    }

    public HeroGlucoseCard()
    {
        InitializeComponent();
    }
}
