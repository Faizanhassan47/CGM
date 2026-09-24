using System.Globalization;

namespace CGM.PatientApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Color.FromArgb("#01B4F1");
    public Color FalseColor { get; set; } = Color.FromArgb("#583295");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string paramStr && paramStr.Contains(':'))
        {
            var parts = paramStr.Split(':');
            var isDark = Application.Current?.RequestedTheme == AppTheme.Dark;
            // Parameters may provide true-light:false-light:true-dark:false-dark.
            // The original two-colour form remains supported.
            var trueIndex = isDark && parts.Length >= 4 ? 2 : 0;
            var falseIndex = isDark && parts.Length >= 4 ? 3 : 1;
            var tColor = Color.FromArgb(parts[trueIndex]);
            var fColor = parts.Length > falseIndex ? Color.FromArgb(parts[falseIndex]) : FalseColor;

            if (value is bool b)
                return b ? tColor : fColor;
        }

        if (value is bool isTrue)
            return isTrue ? TrueColor : FalseColor;

        return TrueColor;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
