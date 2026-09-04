using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Models;

public class SensorInfo
{
    public string SensorId { get; set; } = string.Empty;
    public SensorState State { get; set; } = SensorState.Active;
    public DateTime? ActivationTime { get; set; }
    public DateTime? ExpirationTime { get; set; }
    public TimeSpan? RemainingTime => ExpirationTime.HasValue ? ExpirationTime.Value - DateTime.UtcNow : null;
    public ushort LatestSequenceNumber { get; set; }
    public DateTime? LastReadingTime { get; set; }
    public bool IsWarmUpActive { get; set; }
    public TimeSpan? WarmUpRemaining { get; set; }
}
