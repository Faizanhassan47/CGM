using CGM.PatientApp.Interfaces;

namespace CGM.PatientApp.Services.Cgm;

public sealed class CgmCommandBuilder
{
    private readonly ICgmProtocolParser _parser;
    public CgmCommandBuilder(ICgmProtocolParser parser) => _parser = parser;

    public byte[] BuildE7() => Build(CgmProtocolParser.CmdReadFirmwareVersion);
    public byte[] BuildE8() => Build(CgmProtocolParser.CmdReadSerialNumber);
    public byte[] BuildE1() => Build(CgmProtocolParser.CmdReadBatteryVoltage);
    public byte[] BuildE2() => Build(CgmProtocolParser.CmdReadDeviceTemperature);
    public byte[] BuildE3() => Build(CgmProtocolParser.CmdReadMeasurementState);
    private byte[] Build(byte command) => _parser.BuildCommand(command, ReadOnlySpan<byte>.Empty);
}
