namespace CGM.PatientApp.Models;

public sealed record PagedGlucoseReadings(
    IReadOnlyList<GlucoseMeasurement> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);
