#if ANDROID
using Android;
using Android.Bluetooth;
using Android.Bluetooth.LE;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Java.Util;
using CGM.PatientApp.Enums;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using Microsoft.Maui.ApplicationModel;
using CGM.PatientApp.Services.Config;
using CGM.PatientApp.Platforms.Android.Services;
namespace CGM.PatientApp.Services.Ble;

public sealed class AndroidBleService : IBleService, IDisposable
{
    public static readonly UUID ServiceUuid = UUID.FromString("0000FFF0-0000-1000-8000-00805F9B34FB")!;
    public static readonly UUID CommandUuid = UUID.FromString("0000FFF1-0000-1000-8000-00805F9B34FB")!;
    public static readonly UUID MultiFrameUuid = UUID.FromString("0000FFF2-0000-1000-8000-00805F9B34FB")!;
    private static readonly UUID ClientConfigurationUuid = UUID.FromString("00002902-0000-1000-8000-00805F9B34FB")!;

    private readonly BluetoothManager? _manager;
    private readonly object _gate = new();
    private readonly HashSet<string> _seen = new(StringComparer.OrdinalIgnoreCase);
    private ScanCallbackImpl? _scanCallback;
    private BluetoothGatt? _gatt;
    private GattCallbackImpl? _gattCallback;
    private BluetoothGattCharacteristic? _commandCharacteristic;
    private BluetoothGattCharacteristic? _multiFrameCharacteristic;

    public AndroidBleService()
    {
        _manager = (BluetoothManager?)Android.App.Application.Context.GetSystemService(Context.BluetoothService);
    }

    public event EventHandler<CgmConnectionState>? ConnectionStateChanged;
    public event EventHandler<byte[]>? NotificationReceived;
    public CgmConnectionState CurrentState { get; private set; } = CgmConnectionState.Disconnected;
    public CgmBleDiagnostics Diagnostics { get; private set; } = new();

    public Task<bool> CheckBluetoothEnabledAsync() => Task.FromResult(_manager?.Adapter?.IsEnabled == true);

    public async Task<bool> RequestPermissionsAsync()
    {
        if (!Android.App.Application.Context.PackageManager!.HasSystemFeature(PackageManager.FeatureBluetoothLe))
            return false;

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var scan = await Permissions.RequestAsync<BluetoothScanPermission>();
            var connect = await Permissions.RequestAsync<BluetoothConnectPermission>();
            bool granted = scan == PermissionStatus.Granted && connect == PermissionStatus.Granted;
            SetState(granted ? CgmConnectionState.Disconnected : CgmConnectionState.PermissionDenied);
            return granted;
        }

        var location = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
        bool locationGranted = location == PermissionStatus.Granted;
        SetState(locationGranted ? CgmConnectionState.Disconnected : CgmConnectionState.PermissionDenied);
        return locationGranted;
    }

    public async Task StartScanningAsync(Action<CgmDeviceInfo> onDeviceDiscovered, CancellationToken cancellationToken = default)
    {
        if (!await CheckBluetoothEnabledAsync())
        {
            SetState(CgmConnectionState.BluetoothOff);
            return;
        }

        if (!await RequestPermissionsAsync())
        {
            SetState(CgmConnectionState.PermissionDenied);
            return;
        }

        var scanner = _manager?.Adapter?.BluetoothLeScanner;
        if (scanner is null)
        {
            SetFailure("BLE scanning is unavailable.");
            return;
        }

        await StopScanningAsync();
        lock (_gate) _seen.Clear();
        _scanCallback = new ScanCallbackImpl(this, onDeviceDiscovered);
        var settings = new ScanSettings.Builder().SetScanMode(Android.Bluetooth.LE.ScanMode.LowLatency)!.Build();
        
        System.Diagnostics.Debug.WriteLine("[BLE] Starting BLE scan for CGM devices...");
        scanner.StartScan(null, settings, _scanCallback);
        cancellationToken.Register(() => _ = StopScanningAsync());
        SetState(CgmConnectionState.Scanning);
    }

    public Task StopScanningAsync()
    {
        lock (_gate)
        {
            if (_scanCallback is not null)
            {
                try { _manager?.Adapter?.BluetoothLeScanner?.StopScan(_scanCallback); } catch { }
                _scanCallback.Dispose();
                _scanCallback = null;
                System.Diagnostics.Debug.WriteLine("[BLE] BLE scan stopped.");
            }
        }
        if (CurrentState == CgmConnectionState.Scanning)
        {
            SetState(CgmConnectionState.Disconnected);
        }
        return Task.CompletedTask;
    }

    public async Task<bool> ConnectAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(deviceId)) return false;
        await StopScanningAsync();
        await DisconnectAsync();
        SetState(CgmConnectionState.Connecting);
        try
        {
            var device = _manager?.Adapter?.GetRemoteDevice(deviceId);
            if (device is null)
            {
                SetFailure("The selected CGM is unavailable.");
                return false;
            }
            _gattCallback = new GattCallbackImpl(this);
            System.Diagnostics.Debug.WriteLine($"[BLE] Connecting GATT to {deviceId}...");
            _gatt = OperatingSystem.IsAndroidVersionAtLeast(23)
                ? device.ConnectGatt(Android.App.Application.Context, false, _gattCallback, BluetoothTransports.Le)
                : device.ConnectGatt(Android.App.Application.Context, false, _gattCallback);

            if (_gatt is null)
            {
                SetFailure("Could not create GATT connection.");
                return false;
            }

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(25));
            await _gattCallback.Ready.Task.WaitAsync(timeout.Token);
            return CurrentState == CgmConnectionState.Ready;
        }
        catch (System.OperationCanceledException)
        {
            SetFailure("The CGM connection timed out.");
            return false;
        }
        catch (Exception ex)
        {
            SetFailure($"We couldn't connect to this CGM ({ex.Message}).");
            return false;
        }
    }

    public Task DisconnectAsync()
    {
        _commandCharacteristic = null;
        _multiFrameCharacteristic = null;
        try { CgmBleForegroundService.Stop(Android.App.Application.Context); } catch { }
        try { _gatt?.Disconnect(); _gatt?.Close(); } catch { }
        _gatt?.Dispose(); _gatt = null;
        _gattCallback?.Dispose(); _gattCallback = null;
        if (CurrentState != CgmConnectionState.ConnectionFailed)
        {
            SetState(CgmConnectionState.Disconnected);
        }
        return Task.CompletedTask;
    }

    public async Task<bool> WriteCommandAsync(byte[] commandBytes, CancellationToken cancellationToken = default)
    {
        if (_gatt is null || _commandCharacteristic is null || CurrentState != CgmConnectionState.Ready)
            return false;

        var callback = _gattCallback!;
        callback.ResetWrite();

        var writeType = _commandCharacteristic.Properties.HasFlag(GattProperty.Write)
            ? GattWriteType.Default
            : GattWriteType.NoResponse;

        bool started;
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            started = _gatt.WriteCharacteristic(_commandCharacteristic, commandBytes, (int)writeType) == (int)CurrentBluetoothStatusCodes.Success;
        }
        else
        {
#pragma warning disable CS0618
            _commandCharacteristic.SetValue(commandBytes);
            _commandCharacteristic.WriteType = writeType;
            started = _gatt.WriteCharacteristic(_commandCharacteristic);
#pragma warning restore CS0618
        }

        if (!started) return false;

        if (writeType == GattWriteType.Default)
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(8));
            return await callback.WriteCompleted.Task.WaitAsync(timeout.Token);
        }

        return true;
    }

    private void OnScanResult(ScanResult result, Action<CgmDeviceInfo> callback)
    {
        string name = string.Empty;
        try
        {
            name = result.ScanRecord?.DeviceName ?? string.Empty;
            if (string.IsNullOrWhiteSpace(name) && result.Device is not null)
            {
                name = result.Device.Name ?? string.Empty;
            }
        }
        catch { }

        bool advertisesService = result.ScanRecord?.ServiceUuids?.Any(x => x.Uuid?.Equals(ServiceUuid) == true) == true;
        bool allowSimulator = EnvConfig.GetBool("CGM_ALLOW_BLE_SIMULATOR", false);

        bool isProductionDevice = CgmDeviceFilter.IsCandidateName(name);
        bool isSimulatorDevice = CgmDeviceFilter.IsSimulator(name, advertisesService, allowSimulator);

        if (!isProductionDevice && !isSimulatorDevice)
            return;

        if (result.Device?.Address is not { } address)
            return;

        lock (_gate)
        {
            if (!_seen.Add(address))
                return;
        }

        string finalName = isSimulatorDevice ? "CGM Development Simulator" : (!string.IsNullOrWhiteSpace(name) ? name : "CGM Sensor");

        System.Diagnostics.Debug.WriteLine($"[BLE] Candidate CGM found: '{finalName}' ({address}), RSSI: {result.Rssi}, AdvertisesService: {advertisesService}");
        SetState(CgmConnectionState.DeviceFound);
        callback(new CgmDeviceInfo
        {
            BluetoothId = address,
            DeviceName = finalName,
            Rssi = result.Rssi,
            AdvertisesCgmService = advertisesService,
            ConnectionState = CgmConnectionState.DeviceFound
        });
    }

    private bool ConfigureGatt(BluetoothGatt gatt)
    {
        var service = gatt.GetService(ServiceUuid);
        if (service is null)
        {
            System.Diagnostics.Debug.WriteLine("[BLE] Primary service FFF0 NOT found on device.");
            Diagnostics = new CgmBleDiagnostics
            {
                ServiceFff0Found = false,
                CharacteristicFff1Found = false,
                CharacteristicFff2Found = false,
                LastError = "This device does not appear to be a supported CGM (Service FFF0 not found)."
            };
            SetFailure("This device does not appear to be a supported CGM.");
            return false;
        }

        System.Diagnostics.Debug.WriteLine("[BLE] Primary service FFF0 discovered.");
        _commandCharacteristic = service.GetCharacteristic(CommandUuid);
        _multiFrameCharacteristic = service.GetCharacteristic(MultiFrameUuid);

        bool fff1Found = _commandCharacteristic is not null;
        bool fff2Found = _multiFrameCharacteristic is not null;
        System.Diagnostics.Debug.WriteLine($"[BLE] FFF1 discovered: {fff1Found}, FFF2 discovered: {fff2Found}");

        Diagnostics = new CgmBleDiagnostics
        {
            ServiceFff0Found = true,
            CharacteristicFff1Found = fff1Found,
            CharacteristicFff2Found = fff2Found,
            LastError = !fff2Found ? "Multi-frame characteristic FFF2 is unavailable." : null
        };

        if (!fff1Found)
        {
            System.Diagnostics.Debug.WriteLine("[BLE] Command characteristic FFF1 missing!");
            SetFailure("CGM communication setup failed (FFF1 missing).");
            return false;
        }

        var properties = _commandCharacteristic!.Properties;
        if (!properties.HasFlag(GattProperty.Notify) ||
            !(properties.HasFlag(GattProperty.Write) || properties.HasFlag(GattProperty.WriteNoResponse)))
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] FFF1 lacks required properties (Notify/Write): {properties}");
            SetFailure("CGM communication setup failed (FFF1 does not support required Write/Notify).");
            return false;
        }

        if (_multiFrameCharacteristic is not null)
        {
            gatt.SetCharacteristicNotification(_multiFrameCharacteristic, true);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("[BLE] Multi-frame characteristic FFF2 not found; multi-frame capability unavailable.");
        }

        System.Diagnostics.Debug.WriteLine("[BLE] Subscribing to FFF1 notifications...");
        bool notificationStarted = EnableNotifications(gatt, _commandCharacteristic);
        if (!notificationStarted)
        {
            System.Diagnostics.Debug.WriteLine("[BLE] Failed to write descriptor for FFF1 notification subscription.");
            SetFailure("Failed to enable CGM notifications.");
            return false;
        }

        return true;
    }

    private static bool EnableNotifications(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
    {
        if (!gatt.SetCharacteristicNotification(characteristic, true)) return false;
        var descriptor = characteristic.GetDescriptor(ClientConfigurationUuid);
        if (descriptor is null) return false;
        var enableValue = BluetoothGattDescriptor.EnableNotificationValue?.ToArray() ?? [0x01, 0x00];
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            return gatt.WriteDescriptor(descriptor, enableValue) == (int)CurrentBluetoothStatusCodes.Success;
#pragma warning disable CS0618
        descriptor.SetValue(enableValue);
        return gatt.WriteDescriptor(descriptor);
#pragma warning restore CS0618
    }

    private void SetFailure(string message)
    {
        Diagnostics = new CgmBleDiagnostics
        {
            ServiceFff0Found = Diagnostics.ServiceFff0Found,
            CharacteristicFff1Found = Diagnostics.CharacteristicFff1Found,
            CharacteristicFff2Found = Diagnostics.CharacteristicFff2Found,
            LastError = message
        };
        SetState(CgmConnectionState.ConnectionFailed);
    }

    private void SetState(CgmConnectionState state)
    {
        CurrentState = state;
        ConnectionStateChanged?.Invoke(this, state);
    }

    public void Dispose()
    {
        _ = StopScanningAsync();
        _ = DisconnectAsync();
    }

    internal sealed class ScanCallbackImpl : ScanCallback
    {
        private static ScanCallbackImpl? s_activeInstance;
        private AndroidBleService? _owner;
        private Action<CgmDeviceInfo>? _callback;

        public ScanCallbackImpl(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            if (s_activeInstance != null)
            {
                _owner = s_activeInstance._owner;
                _callback = s_activeInstance._callback;
            }
        }

        public ScanCallbackImpl(AndroidBleService owner, Action<CgmDeviceInfo> callback)
        {
            _owner = owner;
            _callback = callback;
            s_activeInstance = this;
        }

        public override void OnScanResult(ScanCallbackType callbackType, ScanResult? result)
        {
            if (result is not null && _owner is not null && _callback is not null)
            {
                _owner.OnScanResult(result, _callback);
            }
        }

        public override void OnScanFailed(ScanFailure errorCode)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] Scan failed with error code: {errorCode}");
            _owner?.SetFailure("Bluetooth scanning could not be started.");
        }

        protected override void Dispose(bool disposing)
        {
            if (s_activeInstance == this) s_activeInstance = null;
            _owner = null;
            _callback = null;
            base.Dispose(disposing);
        }
    }

    internal sealed class GattCallbackImpl : BluetoothGattCallback
    {
        private static GattCallbackImpl? s_activeInstance;
        private AndroidBleService? _owner;

        public TaskCompletionSource<bool> Ready { get; } = NewSource();
        public TaskCompletionSource<bool> WriteCompleted { get; private set; } = NewSource();

        public void ResetWrite() => WriteCompleted = NewSource();

        public GattCallbackImpl(IntPtr handle, JniHandleOwnership transfer) : base(handle, transfer)
        {
            if (s_activeInstance != null)
            {
                _owner = s_activeInstance._owner;
            }
        }

        public GattCallbackImpl(AndroidBleService owner)
        {
            _owner = owner;
            s_activeInstance = this;
        }

        public override void OnConnectionStateChange(BluetoothGatt? gatt, GattStatus status, ProfileState newState)
        {
            if (gatt is null) return;
            if (status == GattStatus.Success && newState == ProfileState.Connected)
            {
                _owner?.SetState(CgmConnectionState.DiscoveringServices);
                System.Diagnostics.Debug.WriteLine("[BLE] GATT connected. Discovering services...");
                if (!gatt.DiscoverServices())
                {
                    Fail("Service discovery failed to start.");
                }
            }
            else if (newState == ProfileState.Disconnected)
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] GATT disconnected (status: {status}).");
                _owner?.SetState(CgmConnectionState.Disconnected);
                try { CgmBleForegroundService.Stop(Android.App.Application.Context); } catch { }
                Ready.TrySetResult(false);
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"[BLE] GATT connection error: status={status}, newState={newState}");
                Fail("Connection failed.");
            }
        }

        public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        {
            System.Diagnostics.Debug.WriteLine($"[BLE] GATT OnServicesDiscovered: status={status}");
            if (gatt is null || status != GattStatus.Success || _owner == null || !_owner.ConfigureGatt(gatt))
            {
                Fail("This device does not appear to be a supported CGM.");
            }
        }

        public override void OnDescriptorWrite(BluetoothGatt? gatt, BluetoothGattDescriptor? descriptor, GattStatus status)
        {
            Android.Util.Log.Info("CGM_BLE", $"GATT OnDescriptorWrite: status={status}");
            System.Diagnostics.Debug.WriteLine($"[BLE] GATT OnDescriptorWrite: status={status}");
            if (status == GattStatus.Success)
            {
                _owner?.SetState(CgmConnectionState.Ready);
                try { CgmBleForegroundService.Start(Android.App.Application.Context); } catch { }
                Ready.TrySetResult(true);
            }
            else
            {
                Fail("Failed to enable CGM notifications.");
            }
        }

#pragma warning disable CS0618
        public override void OnCharacteristicChanged(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic)
        {
            var val = characteristic?.GetValue();
            if (val is { Length: > 0 } && _owner is not null)
            {
                Android.Util.Log.Info("CGM_BLE", $"RX (legacy): {Convert.ToHexString(val)}");
                System.Diagnostics.Debug.WriteLine($"[BLE] RX (legacy): {Convert.ToHexString(val)}");
                _owner.NotificationReceived?.Invoke(_owner, val);
            }
        }
#pragma warning restore CS0618

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, byte[] value)
        {
            if (value is { Length: > 0 } && _owner is not null)
            {
                Android.Util.Log.Info("CGM_BLE", $"RX (api33): {Convert.ToHexString(value)}");
                System.Diagnostics.Debug.WriteLine($"[BLE] RX: {Convert.ToHexString(value)}");
                _owner.NotificationReceived?.Invoke(_owner, value.ToArray());
            }
        }

        public override void OnCharacteristicWrite(BluetoothGatt? gatt, BluetoothGattCharacteristic? characteristic, GattStatus status)
        {
            Android.Util.Log.Info("CGM_BLE", $"GATT OnCharacteristicWrite: status={status}");
            System.Diagnostics.Debug.WriteLine($"[BLE] GATT OnCharacteristicWrite: status={status}");
            WriteCompleted.TrySetResult(status == GattStatus.Success);
        }

        private void Fail(string reason)
        {
            _owner?.SetFailure(reason);
            Ready.TrySetResult(false);
        }

        protected override void Dispose(bool disposing)
        {
            if (s_activeInstance == this) s_activeInstance = null;
            _owner = null;
            base.Dispose(disposing);
        }

        private static TaskCompletionSource<bool> NewSource() => new(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private sealed class BluetoothScanPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => [(Manifest.Permission.BluetoothScan, true)];
    }

    [System.Runtime.Versioning.SupportedOSPlatform("android31.0")]
    private sealed class BluetoothConnectPermission : Permissions.BasePlatformPermission
    {
        public override (string androidPermission, bool isRuntime)[] RequiredPermissions => [(Manifest.Permission.BluetoothConnect, true)];
    }
}
#endif
