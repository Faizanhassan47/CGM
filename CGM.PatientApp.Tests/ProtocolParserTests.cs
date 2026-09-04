using CGM.PatientApp.Services.Cgm;
using CGM.PatientApp.Services.Ble;
using Xunit;

namespace CGM.PatientApp.Tests;

public class ProtocolParserTests
{
    private readonly CgmProtocolParser _parser = new();

    [Fact]
    public void CalculateChecksum_ReturnsCorrectTwosComplement()
    {
        // Sample frame without checksum: HEAD 0x55, LEN 0x04, CMD 0xE7
        byte[] frameWithoutChecksum = { 0x55, 0x04, 0xE7 };
        // Sum = 0x55 + 0x04 + 0xE7 = 85 + 4 + 231 = 320 = 0x140
        // Two's complement = (-320) & 0xFF = (-64) & 0xFF = 192 = 0xC0
        byte checksum = _parser.CalculateChecksum(frameWithoutChecksum);
        
        Assert.Equal(0xC0, checksum);

        // Verify that summing the frame + checksum mod 256 is 0
        int totalSum = 0x55 + 0x04 + 0xE7 + checksum;
        Assert.Equal(0, totalSum % 256);
    }

    [Fact]
    public void ValidateFrame_ValidAppFrame_ReturnsTrue()
    {
        byte[] cmdFrame = _parser.BuildCommand(CgmProtocolParser.CmdReadFirmwareVersion, Array.Empty<byte>());
        
        bool isValid = _parser.ValidateFrame(cmdFrame);
        Assert.True(isValid);
        Assert.Equal(4, cmdFrame.Length);
        Assert.Equal(CgmProtocolParser.AppHeader, cmdFrame[0]);
        Assert.Equal(0x04, cmdFrame[1]);
        Assert.Equal(CgmProtocolParser.CmdReadFirmwareVersion, cmdFrame[2]);
    }

    [Fact]
    public void ValidateFrame_CorruptedChecksum_ReturnsFalse()
    {
        byte[] cmdFrame = _parser.BuildCommand(CgmProtocolParser.CmdReadFirmwareVersion, Array.Empty<byte>());
        cmdFrame[^1] = (byte)(cmdFrame[^1] ^ 0xFF); // Corrupt checksum
        
        bool isValid = _parser.ValidateFrame(cmdFrame);
        Assert.False(isValid);
    }

    [Fact]
    public void ParseFirmwareVersion_E7Response_ParsesCorrectly()
    {
        // Response: HEAD 0xAA, LEN 0x08, CMD 0xE7, DATA "A100", CHECKSUM
        byte[] payload = { 0x41, 0x31, 0x30, 0x30 }; // "A100"
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadFirmwareVersion, payload);

        string? version = _parser.ParseAsciiString(frame);
        Assert.Equal("A100", version);
    }

    [Fact]
    public void ParseSerialNumber_E8Response_ParsesCorrectly()
    {
        // Response: HEAD 0xAA, LEN 0x10, CMD 0xE8, DATA "26082400C221" (12 ASCII chars), CHECKSUM
        byte[] payload = System.Text.Encoding.ASCII.GetBytes("26082400C221");
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadSerialNumber, payload);

        string? sn = _parser.ParseAsciiString(frame);
        Assert.Equal("26082400C221", sn);
    }

    [Fact]
    public void ParseBatteryVoltage_E1Response_ParsesLittleEndian()
    {
        // Response for 2850 mV (0x0B22): Little-Endian -> 0x22, 0x0B
        byte[] payload = { 0x22, 0x0B };
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadBatteryVoltage, payload);

        ushort? voltage = _parser.ParseBatteryVoltage(frame);
        Assert.NotNull(voltage);
        Assert.Equal((ushort)2850, voltage.Value);
    }

    [Fact]
    public void ParseTemperature_E2Response_ParsesScaledDeciCelsius()
    {
        // Response for 32.4 °C (raw 324 = 0x0144): Little-Endian -> 0x44, 0x01
        byte[] payload = { 0x44, 0x01 };
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadDeviceTemperature, payload);

        double? temp = _parser.ParseTemperature(frame);
        Assert.NotNull(temp);
        Assert.Equal(32.4, temp.Value, 1);
    }

    [Fact]
    public void ParseMeasurementState_E3Response_ParsesStateAndLatestSn()
    {
        // Response: State 0x01 (Measuring), Latest SN 1250 (0x04E2 -> 0xE2, 0x04)
        byte[] payload = { 0x01, 0xE2, 0x04 };
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadMeasurementState, payload);

        var result = _parser.ParseMeasurementState(frame);
        Assert.NotNull(result);
        Assert.True(result.Value.isMeasuring);
        Assert.Equal((ushort)1250, result.Value.latestSn);
    }

    [Fact]
    public void ParseLiveMeasurement_B0Report_ParsesAllFields()
    {
        // Payload:
        // Timestamp: 1699990272 (0x6553CB00 -> 0x00, 0xCB, 0x53, 0x65)
        // SN: 1205 (0x04B5 -> 0xB5, 0x04)
        // Voltage: 2900 mV (0x0B54 -> 0x54, 0x0B)
        // Temperature: 31.8 °C (318 = 0x013E -> 0x3E, 0x01)
        // WE1: 15.25 nA (1525 raw = 0x05F5 -> 0xF5, 0x05)
        byte[] payload = {
            0x00, 0xCB, 0x53, 0x65, // Timestamp = 1699990272
            0xB5, 0x04,             // SN = 1205
            0x54, 0x0B,             // Voltage = 2900
            0x3E, 0x01,             // Temp = 31.8
            0xF5, 0x05              // WE1 = 15.25 nA
        };
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdLiveMeasurementReport, payload);

        var measurement = _parser.ParseLiveMeasurement(frame);
        Assert.NotNull(measurement);
        Assert.Equal(1699990272u, measurement.Timestamp);
        Assert.Equal((ushort)1205, measurement.SequenceNumber);
        Assert.Equal((ushort)2900, measurement.BatteryVoltageMv);
        Assert.Equal(31.8, measurement.TemperatureCelsius, 1);
        Assert.Equal(15.25, measurement.We1NanoAmps, 2);
    }

    [Fact]
    public void ParseDeviceStatus_F0Report_ParsesMeasuringTimeAndSn()
    {
        // Flag: 0x01
        // Measurement Time: 3600 seconds (0x00000E10 -> 0x10, 0x0E, 0x00, 0x00)
        // Latest SN: 60 (0x003C -> 0x3C, 0x00)
        byte[] payload = {
            0x01,                   // Measuring = true
            0x10, 0x0E, 0x00, 0x00, // 3600 sec
            0x3C, 0x00              // SN = 60
        };
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdDeviceStatusReport, payload);

        var status = _parser.ParseMeasurementStatus(frame);
        Assert.NotNull(status);
        Assert.True(status.Value.isMeasuring);
        Assert.Equal(3600u, status.Value.measurementTime);
        Assert.Equal((ushort)60, status.Value.latestSn);
    }

    [Fact]
    public void ParseHistoricalMeasurements_D3Response_ParsesMultipleRecords()
    {
        // 2 records (24 bytes):
        // Rec 1: Timestamp 100, SN 1, Voltage 2800, Temp 300 (30.0C), WE1 1000 (10.0nA)
        // Rec 2: Timestamp 160, SN 2, Voltage 2800, Temp 302 (30.2C), WE1 1050 (10.5nA)
        byte[] payload = {
            // Record 1
            0x64, 0x00, 0x00, 0x00, // TS 100
            0x01, 0x00,             // SN 1
            0xF0, 0x0A,             // Voltage 2800
            0x2C, 0x01,             // Temp 300
            0xE8, 0x03,             // WE1 1000
            // Record 2
            0xA0, 0x00, 0x00, 0x00, // TS 160
            0x02, 0x00,             // SN 2
            0xF0, 0x0A,             // Voltage 2800
            0x2E, 0x01,             // Temp 302
            0x1A, 0x04              // WE1 1050
        };
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadHistoricalData, payload);

        var records = _parser.ParseHistoricalMeasurements(frame);
        Assert.Equal(2, records.Count);
        Assert.Equal((ushort)1, records[0].SequenceNumber);
        Assert.Equal(10.0, records[0].We1NanoAmps, 2);
        Assert.Equal((ushort)2, records[1].SequenceNumber);
        Assert.Equal(10.5, records[1].We1NanoAmps, 2);
    }

    private byte[] BuildDeviceResponse(byte cmd, byte[] payload)
    {
        int totalLen = 3 + payload.Length + 1;
        byte[] frame = new byte[totalLen];
        frame[0] = CgmProtocolParser.DeviceHeader;
        frame[1] = (byte)totalLen;
        frame[2] = cmd;
        payload.CopyTo(frame, 3);
        frame[^1] = _parser.CalculateChecksum(frame.AsSpan(0, totalLen - 1));
        return frame;
    }
    [Theory]
    [InlineData("CGM-0078A3E3C221", true)]
    [InlineData("cgm-0078a3e3c221", true)]
    [InlineData("Headphones", false)]
    [InlineData("CGM", false)]
    [InlineData(null, false)]
    public void CandidateFilter_OnlyAcceptsCgmPrefix(string? name, bool expected) =>
        Assert.Equal(expected, CgmDeviceFilter.IsCandidateName(name));

    [Fact]
    public void ValidateFrame_RejectsTrailingBytesEvenWithValidEmbeddedFrame()
    {
        var valid = BuildDeviceResponse(CgmProtocolParser.CmdReadFirmwareVersion, System.Text.Encoding.ASCII.GetBytes("A100"));
        Assert.False(_parser.ValidateFrame([.. valid, 0x00]));
    }

    [Fact]
    public void TypedParsers_RejectWrongCommandAndWrongPayloadLength()
    {
        var wrongCommand = BuildDeviceResponse(CgmProtocolParser.CmdReadSerialNumber, System.Text.Encoding.ASCII.GetBytes("A100"));
        var shortSerial = BuildDeviceResponse(CgmProtocolParser.CmdReadSerialNumber, System.Text.Encoding.ASCII.GetBytes("C221"));
        Assert.Null(_parser.ParseFirmwareVersion(wrongCommand));
        Assert.Null(_parser.ParseDeviceSerialNumber(shortSerial));
    }

    [Theory]
    [InlineData(0x00)]
    [InlineData(0x12)]
    [InlineData(0x99)]
    [InlineData(0xFF)]
    public void ValidateFrame_InvalidHeader_ReturnsFalse(byte badHeader)
    {
        byte[] frame = { badHeader, 0x04, 0xE7, 0x00 };
        frame[3] = _parser.CalculateChecksum(frame.AsSpan(0, 3));
        Assert.False(_parser.ValidateFrame(frame));
    }

    [Fact]
    public void ValidateFrame_InvalidLength_ReturnsFalse()
    {
        // Length < 4
        Assert.False(_parser.ValidateFrame(new byte[] { 0xAA, 0x03, 0xE7 }));

        // Length field says 6, but array only has 4 bytes
        byte[] shortFrame = { 0xAA, 0x06, 0xE7, 0x00 };
        Assert.False(_parser.ValidateFrame(shortFrame));

        // Length field says 4, but array has 6 bytes
        byte[] longFrame = { 0xAA, 0x04, 0xE7, 0x00, 0x01, 0x02 };
        Assert.False(_parser.ValidateFrame(longFrame));
    }

    [Fact]
    public void ValidateFrame_InvalidChecksum_ReturnsFalse()
    {
        byte[] frame = BuildDeviceResponse(CgmProtocolParser.CmdReadBatteryVoltage, new byte[] { 0x22, 0x0B });
        // Corrupt checksum byte
        frame[^1] = (byte)(frame[^1] + 1);
        Assert.False(_parser.ValidateFrame(frame));
    }

    [Fact]
    public void TypedParsers_UnsupportedOrUnknownCommand_ReturnsNull()
    {
        byte[] unknownCmdFrame = BuildDeviceResponse(0x99, new byte[] { 0x01, 0x02 });
        Assert.Null(_parser.ParseFirmwareVersion(unknownCmdFrame));
        Assert.Null(_parser.ParseDeviceSerialNumber(unknownCmdFrame));
        Assert.Null(_parser.ParseBatteryResult(unknownCmdFrame));
        Assert.Null(_parser.ParseDeviceTemperatureResult(unknownCmdFrame));
        Assert.Null(_parser.ParseMeasurementStateResult(unknownCmdFrame));

        // Device error response 0xFF
        byte[] errorFrame = BuildDeviceResponse(CgmProtocolParser.ErrorCommand, new byte[] { 0x01 });
        Assert.Null(_parser.ParseFirmwareVersion(errorFrame));
        Assert.Null(_parser.ParseDeviceSerialNumber(errorFrame));
        Assert.Null(_parser.ParseBatteryResult(errorFrame));
        Assert.Null(_parser.ParseDeviceTemperatureResult(errorFrame));
        Assert.Null(_parser.ParseMeasurementStateResult(errorFrame));
    }

    [Fact]
    public void LittleEndianDecoding_VerifiesCorrectByteOrder()
    {
        // 2850 in hex is 0x0B22. Little-Endian representation: low byte 0x22, high byte 0x0B.
        byte[] batteryPayload = { 0x22, 0x0B };
        byte[] batteryFrame = BuildDeviceResponse(CgmProtocolParser.CmdReadBatteryVoltage, batteryPayload);
        var battery = _parser.ParseBatteryResult(batteryFrame);
        Assert.NotNull(battery);
        Assert.Equal((ushort)2850, battery.BatteryVoltageMv);

        // 324 (32.4 °C) in hex is 0x0144. Little-Endian: low byte 0x44, high byte 0x01.
        byte[] tempPayload = { 0x44, 0x01 };
        byte[] tempFrame = BuildDeviceResponse(CgmProtocolParser.CmdReadDeviceTemperature, tempPayload);
        var temp = _parser.ParseDeviceTemperatureResult(tempFrame);
        Assert.NotNull(temp);
        Assert.Equal(32.4, temp.DeviceTemperatureC, 1);
    }

    [Fact]
    public void TypedParsers_E7_E8_E1_E2_E3_SucceedWithStrongTyping()
    {
        // E7: Firmware version
        var e7Frame = BuildDeviceResponse(CgmProtocolParser.CmdReadFirmwareVersion, System.Text.Encoding.ASCII.GetBytes("A100"));
        var fw = _parser.ParseFirmwareVersion(e7Frame);
        Assert.NotNull(fw);
        Assert.Equal("A100", fw.FirmwareVersion);

        // E8: Serial number
        var e8Frame = BuildDeviceResponse(CgmProtocolParser.CmdReadSerialNumber, System.Text.Encoding.ASCII.GetBytes("26082400C221"));
        var sn = _parser.ParseDeviceSerialNumber(e8Frame);
        Assert.NotNull(sn);
        Assert.Equal("26082400C221", sn.SerialNumber);
        Assert.Equal("********C221", sn.MaskedSerialNumber);

        // E1: Battery
        var e1Frame = BuildDeviceResponse(CgmProtocolParser.CmdReadBatteryVoltage, new byte[] { 0x22, 0x0B });
        var batt = _parser.ParseBatteryResult(e1Frame);
        Assert.NotNull(batt);
        Assert.Equal((ushort)2850, batt.BatteryVoltageMv);

        // E2: Temperature
        var e2Frame = BuildDeviceResponse(CgmProtocolParser.CmdReadDeviceTemperature, new byte[] { 0x44, 0x01 });
        var temperature = _parser.ParseDeviceTemperatureResult(e2Frame);
        Assert.NotNull(temperature);
        Assert.Equal(32.4, temperature.DeviceTemperatureC, 1);

        // E3: Measurement state
        var e3Frame = BuildDeviceResponse(CgmProtocolParser.CmdReadMeasurementState, new byte[] { 0x01, 0x10, 0x0E, 0x00, 0x00, 0xE2, 0x04 });
        var state = _parser.ParseMeasurementStateResult(e3Frame);
        Assert.NotNull(state);
        Assert.True(state.IsMeasuring);
        Assert.Equal(3600u, state.ElapsedMeasurementTime);
        Assert.Equal((ushort)1250, state.LatestSequenceNumber);
    }

    [Fact]
    public void CgmCommandBuilder_BuildsValidFrames_For_E7_E8_E1_E2_E3()
    {
        var builder = new CgmCommandBuilder(_parser);

        var e7 = builder.BuildE7();
        Assert.True(_parser.ValidateFrame(e7));
        Assert.Equal(CgmProtocolParser.AppHeader, e7[0]);
        Assert.Equal(0x04, e7[1]);
        Assert.Equal(CgmProtocolParser.CmdReadFirmwareVersion, e7[2]);

        var e8 = builder.BuildE8();
        Assert.True(_parser.ValidateFrame(e8));
        Assert.Equal(CgmProtocolParser.CmdReadSerialNumber, e8[2]);

        var e1 = builder.BuildE1();
        Assert.True(_parser.ValidateFrame(e1));
        Assert.Equal(CgmProtocolParser.CmdReadBatteryVoltage, e1[2]);

        var e2 = builder.BuildE2();
        Assert.True(_parser.ValidateFrame(e2));
        Assert.Equal(CgmProtocolParser.CmdReadDeviceTemperature, e2[2]);

        var e3 = builder.BuildE3();
        Assert.True(_parser.ValidateFrame(e3));
        Assert.Equal(CgmProtocolParser.CmdReadMeasurementState, e3[2]);
    }

    [Theory]
    [InlineData("CGM-0078A3E3C221", true)]
    [InlineData("cgm-c221", true)]
    [InlineData("CGM-PROBE-01", true)]
    [InlineData("Living Room TV", false)]
    [InlineData("Sony WH-1000XM4", false)]
    [InlineData("Galaxy Watch5", false)]
    [InlineData("BLE-Beacon-99", false)]
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData(null, false)]
    public void CgmDeviceFilter_ComprehensiveDeviceFiltering(string? name, bool expected)
    {
        Assert.Equal(expected, CgmDeviceFilter.IsCandidateName(name));
    }
}
