namespace CGM.PatientApp.Models;

public sealed class CgmBleDiagnostics
{
    public bool ServiceFff0Found { get; init; }
    public bool CharacteristicFff1Found { get; init; }
    public bool CharacteristicFff2Found { get; init; }
    public string? LastError { get; init; }
}
