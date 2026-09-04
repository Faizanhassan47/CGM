using System.Globalization;

namespace CGM.PatientApp.Converters;

public class BoolToTextConverter : IValueConverter
{
    public string TrueText { get; set; } = "Show";
    public string FalseText { get; set; } = "Hide";

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string paramStr && paramStr.Contains(':'))
        {
            var parts = paramStr.Split(':');
            var trueVal = parts[0];
            var falseVal = parts.Length > 1 ? parts[1] : string.Empty;

            if (value is bool boolVal)
                return boolVal ? trueVal : falseVal;
        }

        if (value is bool b)
            return b ? TrueText : FalseText;

        return TrueText;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
