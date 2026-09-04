namespace CGM.PatientApp.Controls;

public partial class MetricCard : ContentView
{
    public static readonly BindableProperty CardTitleProperty =
        BindableProperty.Create(nameof(CardTitle), typeof(string), typeof(MetricCard), string.Empty);

    public static readonly BindableProperty CardValueProperty =
        BindableProperty.Create(nameof(CardValue), typeof(string), typeof(MetricCard), string.Empty);

    public static readonly BindableProperty CardSubValueProperty =
        BindableProperty.Create(nameof(CardSubValue), typeof(string), typeof(MetricCard), string.Empty);

    public static readonly BindableProperty IconTextProperty =
        BindableProperty.Create(nameof(IconText), typeof(string), typeof(MetricCard), FontAwesomeIcons.Bullseye);

    public static readonly BindableProperty IconBgColorProperty =
        BindableProperty.Create(nameof(IconBgColor), typeof(Color), typeof(MetricCard), Color.FromArgb("#E6F4EA"));

    public static readonly BindableProperty ValueColorProperty =
        BindableProperty.Create(nameof(ValueColor), typeof(Color), typeof(MetricCard), Color.FromArgb("#059669"));

    public string CardTitle
    {
        get => (string)GetValue(CardTitleProperty);
        set => SetValue(CardTitleProperty, value);
    }

    public string CardValue
    {
        get => (string)GetValue(CardValueProperty);
        set => SetValue(CardValueProperty, value);
    }

    public string CardSubValue
    {
        get => (string)GetValue(CardSubValueProperty);
        set => SetValue(CardSubValueProperty, value);
    }

    public string IconText
    {
        get => (string)GetValue(IconTextProperty);
        set => SetValue(IconTextProperty, value);
    }

    public Color IconBgColor
    {
        get => (Color)GetValue(IconBgColorProperty);
        set => SetValue(IconBgColorProperty, value);
    }

    public Color ValueColor
    {
        get => (Color)GetValue(ValueColorProperty);
        set => SetValue(ValueColorProperty, value);
    }

    public MetricCard()
    {
        InitializeComponent();
    }
}
