using CGM.PatientApp.Models;

namespace CGM.PatientApp.Tests;

public sealed class DeviceModelTests
{
    [Fact]
    public void MaskedSerialNumber_OnlyExposesLastFourCharacters()
    {
        var device = new CgmDeviceInfo { SerialNumber = "26082400C221" };
        Assert.Equal("********C221", device.MaskedSerialNumber);
    }

    [Fact]
    public void IsDataStale_ReturnsTrueWhenLastReadingIsOlderThanTenMinutes()
    {
        var device = new CgmDeviceInfo { LastCommunicationTime = DateTime.UtcNow.AddMinutes(-11) };
        Assert.True(device.IsDataStale);
    }

    [Fact]
    public void IsDataStale_ReturnsFalseForRecentReading()
    {
        var device = new CgmDeviceInfo { LastCommunicationTime = DateTime.UtcNow.AddMinutes(-1) };
        Assert.False(device.IsDataStale);
    }
}
