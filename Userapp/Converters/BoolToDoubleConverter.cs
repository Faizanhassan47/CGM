using System.Globalization;

namespace CGM.PatientApp.Converters;

public class BoolToDoubleConverter : IValueConverter
{
    public double TrueValue { get; set; } = 1.0;
    public double FalseValue { get; set; } = 0.45;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (parameter is string paramStr && paramStr.Contains(':'))
        {
            var parts = paramStr.Split(':');
            if (double.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out double tVal) &&
                double.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out double fVal))
            {
                if (value is bool b)
                    return b ? tVal : fVal;
            }
        }

        if (value is bool isTrue)
            return isTrue ? TrueValue : FalseValue;

        return TrueValue;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
