using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using CGM.PatientApp.Services.Ble;
using CGM.PatientApp.Services.Config;
using CGM.PatientApp.Services.Diagnostics;
namespace CGM.PatientApp.Services.Cgm;

public sealed class CgmDeviceService : ICgmDeviceService, IDisposable
{
    private readonly IBleService _ble;
    private readonly ICgmProtocolParser _parser;
    private readonly CgmCommandBuilder _commands;
    private readonly CGM.PatientApp.Services.Sync.ISyncService? _syncService;
    private readonly IForegroundMonitoringService? _foregroundService;
    private readonly object _responseGate = new();
    private TaskCompletionSource<byte[]>? _pendingResponse;
    private byte _pendingCommand;

    public CgmDeviceService(IBleService ble, ICgmProtocolParser parser, CgmCommandBuilder commands, CGM.PatientApp.Services.Sync.ISyncService? syncService = null, IForegroundMonitoringService? foregroundService = null)
    {
        _ble = ble; _parser = parser; _commands = commands; _syncService = syncService; _foregroundService = foregroundService;
        _ble.NotificationReceived += OnNotification;
        _ble.ConnectionStateChanged += (_, state) =>
        {
            if (state == CgmConnectionState.Disconnected)
            {
                _foregroundService?.StopService();
            }
            ConnectionStateChanged?.Invoke(this, state);
        };
    }

    public event EventHandler<CgmRawMeasurement>? RawMeasurementReceived;
    public event EventHandler<CgmConnectionState>? ConnectionStateChanged;
    public CgmConnectionState State => _ble.CurrentState;
    public CgmDeviceInfo? ConnectedDevice { get; private set; }

    public async Task<CgmDeviceInfo?> ConnectAndVerifyAsync(string bluetoothId, string advertisedName, CancellationToken cancellationToken = default)
    {
        bool allowSimulator = EnvConfig.GetBool("CGM_ALLOW_BLE_SIMULATOR", false);
        bool isSimulator = allowSimulator && advertisedName == "CGM Development Simulator";

        if (!CgmDeviceFilter.IsCandidateName(advertisedName) && !isSimulator)
        {
            System.Diagnostics.Debug.WriteLine($"[CGM] '{advertisedName}' is not a candidate CGM name.");
            return null;
        }

        try
        {
            System.Diagnostics.Debug.WriteLine($"[CGM] Connecting to {bluetoothId} ({advertisedName})...");
            if (!await _ble.ConnectAsync(bluetoothId, cancellationToken))
            {
                System.Diagnostics.Debug.WriteLine("[CGM] BLE connection failed.");
                return null;
            }

            // Step 1: E7 Firmware
            System.Diagnostics.Debug.WriteLine("[CGM] Executing E7 (Firmware)...");
            var firmware = await ExchangeAsync(_commands.BuildE7(), CgmProtocolParser.CmdReadFirmwareVersion, cancellationToken);
            var firmwareResult = _parser.ParseFirmwareVersion(firmware);
            if (firmwareResult is null)
            {
                System.Diagnostics.Debug.WriteLine("[CGM] Failed to parse E7 firmware version.");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[CGM] Firmware: {firmwareResult.FirmwareVersion}");

            // Step 2: E8 Serial Number
            System.Diagnostics.Debug.WriteLine("[CGM] Executing E8 (Serial Number)...");
            var serial = await ExchangeAsync(_commands.BuildE8(), CgmProtocolParser.CmdReadSerialNumber, cancellationToken);
            var serialResult = _parser.ParseDeviceSerialNumber(serial);
            if (serialResult is null)
            {
                System.Diagnostics.Debug.WriteLine("[CGM] Failed to parse E8 serial number.");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[CGM] Serial: {serialResult.MaskedSerialNumber}");

            // Step 3: E1 Battery Voltage
            System.Diagnostics.Debug.WriteLine("[CGM] Executing E1 (Battery Voltage)...");
            var battery = await ExchangeAsync(_commands.BuildE1(), CgmProtocolParser.CmdReadBatteryVoltage, cancellationToken);
            var batteryResult = _parser.ParseBatteryResult(battery);
            if (batteryResult is null)
            {
                System.Diagnostics.Debug.WriteLine("[CGM] Failed to parse E1 battery voltage.");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[CGM] Battery Voltage: {batteryResult.BatteryVoltageMv} mV");

            // Step 4: E2 Device Temperature
            System.Diagnostics.Debug.WriteLine("[CGM] Executing E2 (Device Temperature)...");
            var temperature = await ExchangeAsync(_commands.BuildE2(), CgmProtocolParser.CmdReadDeviceTemperature, cancellationToken);
            var temperatureResult = _parser.ParseDeviceTemperatureResult(temperature);
            if (temperatureResult is null)
            {
                System.Diagnostics.Debug.WriteLine("[CGM] Failed to parse E2 device temperature.");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[CGM] Device Temperature: {temperatureResult.DeviceTemperatureC} °C");

            // Step 5: E3 Measurement Status + Latest SN
            System.Diagnostics.Debug.WriteLine("[CGM] Executing E3 (Measurement State)...");
            var state = await ExchangeAsync(_commands.BuildE3(), CgmProtocolParser.CmdReadMeasurementState, cancellationToken);
            var stateResult = _parser.ParseMeasurementStateResult(state);
            if (stateResult is null)
            {
                System.Diagnostics.Debug.WriteLine("[CGM] Failed to parse E3 measurement state.");
                return null;
            }
            System.Diagnostics.Debug.WriteLine($"[CGM] Measurement State: IsMeasuring={stateResult.IsMeasuring}, ElapsedTime={stateResult.ElapsedMeasurementTime}s, LatestSN={stateResult.LatestSequenceNumber}");

            ConnectedDevice = new CgmDeviceInfo
            {
                BluetoothId = bluetoothId,
                DeviceName = advertisedName,
                FirmwareVersion = firmwareResult.FirmwareVersion,
                SerialNumber = serialResult.SerialNumber,
                BatteryVoltageMv = batteryResult.BatteryVoltageMv,
                BatteryStatusText = batteryResult.BatteryVoltageMv >= 2800 ? "Good" : batteryResult.BatteryVoltageMv >= 2500 ? "Normal" : "Low",
                TemperatureCelsius = temperatureResult.DeviceTemperatureC,
                ConnectionState = CgmConnectionState.Ready,
                LastCommunicationTime = DateTime.UtcNow
            };

            System.Diagnostics.Debug.WriteLine("[CGM] Device successfully verified (E7-E3 complete).");
            _foregroundService?.StartService(ConnectedDevice.DeviceName);
            return ConnectedDevice;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[CGM] ConnectAndVerifyAsync failed: {ex.Message}");
            return null;
        }
    }

    private async Task<byte[]> ExchangeAsync(byte[] command, byte expectedCommand, CancellationToken cancellationToken)
    {
        TaskCompletionSource<byte[]> pending = new(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_responseGate) { _pendingCommand = expectedCommand; _pendingResponse = pending; }
        if (!await _ble.WriteCommandAsync(command, cancellationToken))
            throw new InvalidOperationException($"The CGM command 0x{expectedCommand:X2} could not be sent.");

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(8));
        try
        {
            return await pending.Task.WaitAsync(timeout.Token);
        }
        finally
        {
            lock (_responseGate) if (ReferenceEquals(_pendingResponse, pending)) _pendingResponse = null;
        }
    }

    private void OnNotification(object? sender, byte[] frame)
    {
        System.Diagnostics.Debug.WriteLine($"[CGM] OnNotification received: {Convert.ToHexString(frame)}");
        if (!_parser.ValidateFrame(frame))
        {
            System.Diagnostics.Debug.WriteLine($"[CGM] Frame failed validation (Length={frame.Length})");
            return;
        }

        if (frame[0] != CgmProtocolParser.DeviceHeader || frame[2] == CgmProtocolParser.ErrorCommand)
        {
            System.Diagnostics.Debug.WriteLine($"[CGM] Frame header or error command mismatch (Header=0x{frame[0]:X2}, Cmd=0x{frame[2]:X2})");
            return;
        }
        
        // Handle B0 asynchronous notifications
        if (frame[2] == CgmProtocolParser.CmdLiveMeasurementReport)
        {
            var measurement = _parser.ParseLiveMeasurement(frame);
            if (measurement != null)
            {
                RawMeasurementReceived?.Invoke(this, measurement);
                _foregroundService?.UpdateReading(ConnectedDevice?.DeviceName ?? "CGM Sensor", measurement.GlucoseValueMgDl);
                if (_syncService is not null)
                    SafeAsync.Run(() => _syncService.EnqueueMeasurementAsync(measurement), "CgmDeviceService.EnqueueMeasurement");
            }
            return;
        }

        TaskCompletionSource<byte[]>? pending = null;
        lock (_responseGate)
        {
            if (_pendingResponse is not null && frame[2] == _pendingCommand)
            {
                pending = _pendingResponse;
                _pendingResponse = null;
            }
        }
        pending?.TrySetResult(frame);
    }

    public Task<bool> InitializeAsync() => Task.FromResult(State == CgmConnectionState.Ready);
    public async Task<string> ReadFirmwareVersionAsync() => (await ReadParsed(_commands.BuildE7(), CgmProtocolParser.CmdReadFirmwareVersion, x => _parser.ParseFirmwareVersion(x)?.FirmwareVersion))!;
    public async Task<string> ReadSerialNumberAsync() => (await ReadParsed(_commands.BuildE8(), CgmProtocolParser.CmdReadSerialNumber, x => _parser.ParseDeviceSerialNumber(x)?.SerialNumber))!;
    public async Task<ushort> ReadBatteryVoltageAsync() => (await ReadParsed(_commands.BuildE1(), CgmProtocolParser.CmdReadBatteryVoltage, x => _parser.ParseBatteryResult(x)?.BatteryVoltageMv)).GetValueOrDefault();
    public async Task<double> ReadDeviceTemperatureAsync() => (await ReadParsed(_commands.BuildE2(), CgmProtocolParser.CmdReadDeviceTemperature, x => _parser.ParseDeviceTemperatureResult(x)?.DeviceTemperatureC)).GetValueOrDefault();
    public async Task<(bool isMeasuring, ushort latestSn)> ReadMeasurementStateAsync() { var value = await ReadParsed(_commands.BuildE3(), CgmProtocolParser.CmdReadMeasurementState, x => _parser.ParseMeasurementStateResult(x)); return value is null ? default : (value.IsMeasuring, value.LatestSequenceNumber); }
    private async Task<T?> ReadParsed<T>(byte[] command, byte cmd, Func<byte[], T?> parse) => parse(await ExchangeAsync(command, cmd, CancellationToken.None));
    
    public async Task<bool> StartMeasurementAsync() 
    {
        try 
        {
            await ExchangeAsync(_commands.BuildD1(), CgmProtocolParser.CmdStartMeasurement, CancellationToken.None);
            return true;
        }
        catch 
        {
            return false;
        }
    }
    
    public async Task<IReadOnlyList<CgmRawMeasurement>> ReadHistoricalDataAsync(ushort startSn, ushort endSn)
    {
        try 
        {
            var response = await ExchangeAsync(_commands.BuildD3(), CgmProtocolParser.CmdReadHistoricalData, CancellationToken.None);
            return _parser.ParseHistoricalMeasurements(response);
        }
        catch 
        {
            return Array.Empty<CgmRawMeasurement>();
        }
    }
    
    public void Dispose() => _ble.NotificationReceived -= OnNotification;
}
