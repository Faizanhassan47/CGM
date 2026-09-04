using System.Buffers.Binary;
using System.Text;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Cgm;

public class CgmProtocolParser : ICgmProtocolParser
{
    public const byte AppHeader = 0x55;
    public const byte DeviceHeader = 0xAA;
    public const byte ErrorCommand = 0xFF;

    // Command Identifiers
    public const byte CmdStartMeasurement = 0xD1;
    public const byte CmdReadHistoricalData = 0xD3;
    public const byte CmdReadSingleMeasurement = 0xD4;
    public const byte CmdReadFlashSpace = 0xE0;
    public const byte CmdReadBatteryVoltage = 0xE1;
    public const byte CmdReadDeviceTemperature = 0xE2;
    public const byte CmdReadMeasurementState = 0xE3;
    public const byte CmdReadHallState = 0xE4;
    public const byte CmdReadResetInfo = 0xE5;
    public const byte CmdReadSensorCharacteristics = 0xE6;
    public const byte CmdReadFirmwareVersion = 0xE7;
    public const byte CmdReadSerialNumber = 0xE8;
    public const byte CmdLiveMeasurementReport = 0xB0;
    public const byte CmdDeviceStatusReport = 0xF0;

    /// <summary>
    /// Calculates the protocol checksum: Two's complement of the sum of bytes from HEAD through the last data byte.
    /// </summary>
    public byte CalculateChecksum(ReadOnlySpan<byte> frameWithoutChecksum)
    {
        int sum = 0;
        for (int i = 0; i < frameWithoutChecksum.Length; i++)
        {
            sum += frameWithoutChecksum[i];
        }
        return (byte)(-sum & 0xFF);
    }

    /// <summary>
    /// Validates frame header (0xAA or 0x55), length field, and checksum.
    /// </summary>
    public bool ValidateFrame(ReadOnlySpan<byte> frame)
    {
        // Minimal frame: HEAD(1) + LEN(1) + CMD(1) + CHECKSUM(1) = 4 bytes
        if (frame.Length < 4)
            return false;

        byte header = frame[0];
        if (header != DeviceHeader && header != AppHeader)
            return false;

        byte len = frame[1];
        // Length field represents total length from HEAD to CHECKSUM
        if (frame.Length != len || len < 4)
            return false;

        var frameSlice = frame[..len];
        int sum = 0;
        for (int i = 0; i < frameSlice.Length; i++)
        {
            sum += frameSlice[i];
        }

        // Sum of all bytes including two's complement checksum must be 0 mod 256
        return (sum & 0xFF) == 0;
    }

    /// <summary>
    /// Builds an application request frame (HEAD 0x55, LEN, CMD, DATA, CHECKSUM).
    /// </summary>
    public byte[] BuildCommand(byte cmd, ReadOnlySpan<byte> data)
    {
        int totalLen = 3 + data.Length + 1; // HEAD(1) + LEN(1) + CMD(1) + DATA(N) + CHECKSUM(1)
        byte[] frame = new byte[totalLen];

        frame[0] = AppHeader;
        frame[1] = (byte)totalLen;
        frame[2] = cmd;

        if (data.Length > 0)
        {
            data.CopyTo(frame.AsSpan(3, data.Length));
        }

        frame[^1] = CalculateChecksum(frame.AsSpan(0, totalLen - 1));
        return frame;
    }

    /// <summary>
    /// Parses B0 live measurement report packet.
    /// Payload: Timestamp(4), SN(2), Battery(2), Temperature(2), WE1(2), Reserved(N)
    /// </summary>
    public CgmRawMeasurement? ParseLiveMeasurement(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame))
            return null;

        if (frame[2] != CmdLiveMeasurementReport)
            return null;

        var payload = frame[3..^1];
        if (payload.Length < 12)
            return null;

        uint timestamp = BinaryPrimitives.ReadUInt32LittleEndian(payload[0..4]);
        ushort sn = BinaryPrimitives.ReadUInt16LittleEndian(payload[4..6]);
        ushort voltage = BinaryPrimitives.ReadUInt16LittleEndian(payload[6..8]);
        short tempRaw = BinaryPrimitives.ReadInt16LittleEndian(payload[8..10]);
        short we1Raw = BinaryPrimitives.ReadInt16LittleEndian(payload[10..12]);

        byte[]? reserved = null;
        if (payload.Length > 12)
        {
            reserved = payload[12..].ToArray();
        }

        return new CgmRawMeasurement
        {
            Timestamp = timestamp,
            SequenceNumber = sn,
            BatteryVoltageMv = voltage,
            RawTemperature = tempRaw,
            RawWe1 = we1Raw,
            ReservedBytes = reserved,
            ReceivedTime = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Parses F0 automatic device measurement status report.
    /// Payload: Flag(1), Measurement Time(4), Latest SN(2)
    /// </summary>
    public (bool isMeasuring, uint measurementTime, ushort latestSn)? ParseMeasurementStatus(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame))
            return null;

        if (frame[2] != CmdDeviceStatusReport)
            return null;

        var payload = frame[3..^1];
        if (payload.Length < 7)
            return null;

        bool isMeasuring = payload[0] == 0x01;
        uint measurementTime = BinaryPrimitives.ReadUInt32LittleEndian(payload[1..5]);
        ushort latestSn = BinaryPrimitives.ReadUInt16LittleEndian(payload[5..7]);

        return (isMeasuring, measurementTime, latestSn);
    }

    /// <summary>
    /// Parses ASCII string responses such as E7 (Firmware, 4 bytes) and E8 (Serial Number, 12 bytes).
    /// </summary>
    public string? ParseAsciiString(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame) || frame[0] != DeviceHeader)
            return null;

        var payload = frame[3..^1];
        if (payload.IsEmpty)
            return string.Empty;

        return Encoding.ASCII.GetString(payload).Trim('\0', ' ');
    }

    public FirmwareVersionResult? ParseFirmwareVersion(ReadOnlySpan<byte> frame) =>
        IsResponse(frame, CmdReadFirmwareVersion, 4) ? new(ParseAsciiString(frame)!) : null;

    public DeviceSerialNumberResult? ParseDeviceSerialNumber(ReadOnlySpan<byte> frame) =>
        IsResponse(frame, CmdReadSerialNumber, 12) ? new(ParseAsciiString(frame)!) : null;

    public BatteryVoltageResult? ParseBatteryResult(ReadOnlySpan<byte> frame) =>
        IsResponse(frame, CmdReadBatteryVoltage, 2)
            ? new(BinaryPrimitives.ReadUInt16LittleEndian(frame[3..5]))
            : null;

    public DeviceTemperatureResult? ParseDeviceTemperatureResult(ReadOnlySpan<byte> frame)
    {
        if (!IsResponse(frame, CmdReadDeviceTemperature, 2)) return null;
        return new(BinaryPrimitives.ReadInt16LittleEndian(frame[3..5]));
    }

    public MeasurementStateResult? ParseMeasurementStateResult(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame) || frame[0] != DeviceHeader || frame[2] != CmdReadMeasurementState) return null;
        var payload = frame[3..^1];
        if (payload.Length < 3 || payload[0] > 1) return null;
        if (payload.Length >= 7) return new(payload[0] == 1, BinaryPrimitives.ReadUInt32LittleEndian(payload[1..5]), BinaryPrimitives.ReadUInt16LittleEndian(payload[5..7]));
        return new(payload[0] == 1, 0, BinaryPrimitives.ReadUInt16LittleEndian(payload[1..3]));
    }

    private bool IsResponse(ReadOnlySpan<byte> frame, byte command, int payloadLength) =>
        ValidateFrame(frame) && frame[0] == DeviceHeader && frame[2] == command && frame[1] == payloadLength + 4;

    /// <summary>
    /// Parses E1 battery voltage response (uint16 mV in Little-Endian).
    /// </summary>
    public ushort? ParseBatteryVoltage(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame) || frame[0] != DeviceHeader || frame[2] != CmdReadBatteryVoltage)
            return null;

        var payload = frame[3..^1];
        if (payload.Length < 2)
            return null;

        return BinaryPrimitives.ReadUInt16LittleEndian(payload[0..2]);
    }

    /// <summary>
    /// Parses E2 device temperature response (int16 raw / 10.0 °C).
    /// </summary>
    public double? ParseTemperature(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame) || frame[0] != DeviceHeader || frame[2] != CmdReadDeviceTemperature)
            return null;

        var payload = frame[3..^1];
        if (payload.Length < 2)
            return null;

        short rawTemp = BinaryPrimitives.ReadInt16LittleEndian(payload[0..2]);
        return rawTemp / 10.0;
    }

    /// <summary>
    /// Parses E3 measurement state response (state byte 1/0, latest SN uint16).
    /// </summary>
    public (bool isMeasuring, ushort latestSn)? ParseMeasurementState(ReadOnlySpan<byte> frame)
    {
        if (!ValidateFrame(frame) || frame[0] != DeviceHeader || frame[2] != CmdReadMeasurementState)
            return null;

        var payload = frame[3..^1];
        if (payload.Length < 3 || payload[0] > 1)
            return null;

        bool isMeasuring = payload[0] == 0x01;
        ushort latestSn = payload.Length >= 7
            ? BinaryPrimitives.ReadUInt16LittleEndian(payload[5..7])
            : BinaryPrimitives.ReadUInt16LittleEndian(payload[1..3]);

        return (isMeasuring, latestSn);
    }

    /// <summary>
    /// Parses D3 multi-frame historical measurement records.
    /// Each record is 12 bytes: Timestamp(4), SN(2), Voltage(2), Temp(2), WE1(2)
    /// </summary>
    public IReadOnlyList<CgmRawMeasurement> ParseHistoricalMeasurements(ReadOnlySpan<byte> frame)
    {
        var list = new List<CgmRawMeasurement>();
        if (!ValidateFrame(frame))
            return list;

        if (frame[2] != CmdReadHistoricalData)
            return list;

        var payload = frame[3..^1];
        const int recordSize = 12;
        int count = payload.Length / recordSize;

        for (int i = 0; i < count; i++)
        {
            var recSlice = payload.Slice(i * recordSize, recordSize);
            uint timestamp = BinaryPrimitives.ReadUInt32LittleEndian(recSlice[0..4]);
            ushort sn = BinaryPrimitives.ReadUInt16LittleEndian(recSlice[4..6]);
            ushort voltage = BinaryPrimitives.ReadUInt16LittleEndian(recSlice[6..8]);
            short tempRaw = BinaryPrimitives.ReadInt16LittleEndian(recSlice[8..10]);
            short we1Raw = BinaryPrimitives.ReadInt16LittleEndian(recSlice[10..12]);

            list.Add(new CgmRawMeasurement
            {
                Timestamp = timestamp,
                SequenceNumber = sn,
                BatteryVoltageMv = voltage,
                RawTemperature = tempRaw,
                RawWe1 = we1Raw,
                ReceivedTime = DateTime.UtcNow
            });
        }

        return list;
    }
}
