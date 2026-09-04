using System.Globalization;

namespace CGM.PatientApp.Converters;

public class BoolToColorConverter : IValueConverter
{
    public Color TrueColor { get; set; } = Color.FromArgb("#0D9488");
    public Color FalseColor { get; set; } = Color.FromArgb("#94A3B8");

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string paramStr && paramStr.Contains(':'))
        {
            var parts = paramStr.Split(':');
            var tColor = Color.FromArgb(parts[0]);
            var fColor = parts.Length > 1 ? Color.FromArgb(parts[1]) : FalseColor;

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
