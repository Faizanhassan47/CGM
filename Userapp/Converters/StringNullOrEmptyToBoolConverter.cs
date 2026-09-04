using System.Globalization;

namespace CGM.PatientApp.Converters;

public class StringNullOrEmptyToBoolConverter : IValueConverter
{
    public bool Invert { get; set; } = false;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isEmpty = string.IsNullOrWhiteSpace(value as string);
        return Invert ? !isEmpty : isEmpty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
