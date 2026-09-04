using CGM.PatientApp.Enums;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IGlucoseCalculationService
{
    bool IsAlgorithmAvailable { get; }
    GlucoseMeasurement CalculateFinalGlucose(CgmRawMeasurement rawMeasurement, GlucoseUnit targetUnit);
    GlucoseTrend CalculateTrend(IEnumerable<GlucoseMeasurement> recentReadings);
}
