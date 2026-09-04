using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Services.Cgm;

public static class GlucoseUnitConverter
{
    private const double MgDlPerMmolL = 18.0182;

    public static double Convert(double value, GlucoseUnit from, GlucoseUnit to)
    {
        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
            throw new ArgumentOutOfRangeException(nameof(value), "Glucose must be a finite, non-negative value.");
        if (from == to) return value;
        return to == GlucoseUnit.MmolL
            ? Math.Round(value / MgDlPerMmolL, 1, MidpointRounding.AwayFromZero)
            : Math.Round(value * MgDlPerMmolL, 0, MidpointRounding.AwayFromZero);
    }
}
