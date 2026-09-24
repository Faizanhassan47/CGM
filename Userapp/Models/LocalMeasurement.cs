using SQLite;

namespace CGM.PatientApp.Models;

[Table("LocalMeasurements")]
public class LocalMeasurement
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed]
    public string MeasurementId { get; set; } = Guid.NewGuid().ToString();

    public int SequenceNumber { get; set; }

    public double GlucoseValue { get; set; }

    public DateTime MeasuredAt { get; set; } = DateTime.UtcNow;

    public string? Trend { get; set; }

    /// <summary>
    /// Status: "Pending", "Synced", or "Failed"
    /// </summary>
    [Indexed]
    public string SyncStatus { get; set; } = "Pending";

    public int RetryCount { get; set; }

    public string? LastError { get; set; }

    public DateTime? SyncedAt { get; set; }

    public int BatteryVoltageMv { get; set; }

    public double DeviceTemperatureC { get; set; }

    public double We1NanoAmps { get; set; }
}
