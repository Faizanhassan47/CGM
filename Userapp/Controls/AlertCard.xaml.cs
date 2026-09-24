namespace CGM.PatientApp.Controls;

using System.Windows.Input;

public partial class AlertCard : ContentView
{
    public static readonly BindableProperty CommandProperty =
        BindableProperty.Create(nameof(Command), typeof(ICommand), typeof(AlertCard));

    public static readonly BindableProperty CommandParameterProperty =
        BindableProperty.Create(nameof(CommandParameter), typeof(object), typeof(AlertCard));
    public static readonly BindableProperty AlertTitleProperty =
        BindableProperty.Create(nameof(AlertTitle), typeof(string), typeof(AlertCard), string.Empty);

    public static readonly BindableProperty AlertValueProperty =
        BindableProperty.Create(nameof(AlertValue), typeof(string), typeof(AlertCard), string.Empty, propertyChanged: (b, o, n) => ((AlertCard)b).HasValue = !string.IsNullOrEmpty((string)n));

    public static readonly BindableProperty AlertDescriptionProperty =
        BindableProperty.Create(nameof(AlertDescription), typeof(string), typeof(AlertCard), string.Empty, propertyChanged: (b, o, n) => ((AlertCard)b).HasDescription = !string.IsNullOrEmpty((string)n));

    public static readonly BindableProperty AlertTimeProperty =
        BindableProperty.Create(nameof(AlertTime), typeof(string), typeof(AlertCard), string.Empty);

    public static readonly BindableProperty BadgeTextProperty =
        BindableProperty.Create(nameof(BadgeText), typeof(string), typeof(AlertCard), "Info");

    public static readonly BindableProperty SubBadgeTextProperty =
        BindableProperty.Create(nameof(SubBadgeText), typeof(string), typeof(AlertCard), string.Empty, propertyChanged: (b, o, n) => ((AlertCard)b).HasSubBadge = !string.IsNullOrEmpty((string)n));

    public static readonly BindableProperty IconTextProperty =
        BindableProperty.Create(nameof(IconText), typeof(string), typeof(AlertCard), FontAwesomeIcons.Info);

    public static readonly BindableProperty IconBgColorProperty =
        BindableProperty.Create(nameof(IconBgColor), typeof(Color), typeof(AlertCard), Color.FromArgb("#FFFFFF"));

    public static readonly BindableProperty IconTextColorProperty =
        BindableProperty.Create(nameof(IconTextColor), typeof(Color), typeof(AlertCard), Color.FromArgb("#01B4F1"));

    public static readonly BindableProperty BadgeBgColorProperty =
        BindableProperty.Create(nameof(BadgeBgColor), typeof(Color), typeof(AlertCard), Color.FromArgb("#FFFFFF"));

    public static readonly BindableProperty BadgeTextColorProperty =
        BindableProperty.Create(nameof(BadgeTextColor), typeof(Color), typeof(AlertCard), Color.FromArgb("#01B4F1"));

    public static readonly BindableProperty DotColorProperty =
        BindableProperty.Create(nameof(DotColor), typeof(Color), typeof(AlertCard), Color.FromArgb("#01B4F1"));

    public static readonly BindableProperty HasValueProperty =
        BindableProperty.Create(nameof(HasValue), typeof(bool), typeof(AlertCard), false);

    public static readonly BindableProperty HasDescriptionProperty =
        BindableProperty.Create(nameof(HasDescription), typeof(bool), typeof(AlertCard), false);

    public static readonly BindableProperty HasSubBadgeProperty =
        BindableProperty.Create(nameof(HasSubBadge), typeof(bool), typeof(AlertCard), false);

    public string AlertTitle { get => (string)GetValue(AlertTitleProperty); set => SetValue(AlertTitleProperty, value); }
    public string AlertValue { get => (string)GetValue(AlertValueProperty); set => SetValue(AlertValueProperty, value); }
    public string AlertDescription { get => (string)GetValue(AlertDescriptionProperty); set => SetValue(AlertDescriptionProperty, value); }
    public string AlertTime { get => (string)GetValue(AlertTimeProperty); set => SetValue(AlertTimeProperty, value); }
    public string BadgeText { get => (string)GetValue(BadgeTextProperty); set => SetValue(BadgeTextProperty, value); }
    public string SubBadgeText { get => (string)GetValue(SubBadgeTextProperty); set => SetValue(SubBadgeTextProperty, value); }
    public string IconText { get => (string)GetValue(IconTextProperty); set => SetValue(IconTextProperty, value); }
    public Color IconBgColor { get => (Color)GetValue(IconBgColorProperty); set => SetValue(IconBgColorProperty, value); }
    public Color IconTextColor { get => (Color)GetValue(IconTextColorProperty); set => SetValue(IconTextColorProperty, value); }
    public Color BadgeBgColor { get => (Color)GetValue(BadgeBgColorProperty); set => SetValue(BadgeBgColorProperty, value); }
    public Color BadgeTextColor { get => (Color)GetValue(BadgeTextColorProperty); set => SetValue(BadgeTextColorProperty, value); }
    public Color DotColor { get => (Color)GetValue(DotColorProperty); set => SetValue(DotColorProperty, value); }
    public bool HasValue { get => (bool)GetValue(HasValueProperty); set => SetValue(HasValueProperty, value); }
    public bool HasDescription { get => (bool)GetValue(HasDescriptionProperty); set => SetValue(HasDescriptionProperty, value); }
    public bool HasSubBadge { get => (bool)GetValue(HasSubBadgeProperty); set => SetValue(HasSubBadgeProperty, value); }
    public ICommand? Command { get => (ICommand?)GetValue(CommandProperty); set => SetValue(CommandProperty, value); }
    public object? CommandParameter { get => GetValue(CommandParameterProperty); set => SetValue(CommandParameterProperty, value); }

    public AlertCard()
    {
        InitializeComponent();
    }
}
