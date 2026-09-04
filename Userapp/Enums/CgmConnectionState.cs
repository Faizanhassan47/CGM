namespace CGM.PatientApp.Enums;

public enum CgmConnectionState
{
    BluetoothOff = 0,
    PermissionRequired = 1,
    NoDeviceConfigured = 2,
    Scanning = 3,
    DeviceFound = 4,
    Connecting = 5,
    Connected = 6,
    DiscoveringServices = 7,
    Ready = 8,
    Synchronizing = 9,
    Disconnected = 10,
    Reconnecting = 11,
    ConnectionFailed = 12,
    PermissionDenied = 13
}
