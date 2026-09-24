using System.Net.Http.Headers;
using System.Net.Http.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;
using System.Text.Json;
using CGM.PatientApp.Enums;

namespace CGM.PatientApp.Services.Cgm;

public class ApiGlucoseService : IGlucoseService
{
    private readonly HttpClient _client;
    
    public event EventHandler<GlucoseMeasurement>? GlucoseReadingReceived;

    public ApiGlucoseService(HttpClient client)
    {
        _client = client;
    }

    private static async Task<HttpRequestMessage> AuthorizedAsync(HttpMethod method, string path)
    {
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");
        var request = new HttpRequestMessage(method, path);
        if (!string.IsNullOrWhiteSpace(token))
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return request;
    }

    public async Task<GlucoseSummary> GetDashboardSummaryAsync()
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, "api/glucose/summary");
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var dto = await response.Content.ReadFromJsonAsync<BackendSummaryDto>();
                if (dto != null)
                {
                    var unit = Enum.TryParse<GlucoseUnit>(dto.GlucoseUnit, true, out var parsedUnit) ? parsedUnit : GlucoseUnit.MgDl;
                    var trend = Enum.TryParse<GlucoseTrend>(dto.Trend, true, out var parsedTrend) ? parsedTrend : GlucoseTrend.Stable;
                    var status = Enum.TryParse<GlucoseStatus>(dto.Status, true, out var parsedStatus) ? parsedStatus : GlucoseStatus.Normal;

                    return new GlucoseSummary
                    {
                        LatestReading = dto.CurrentGlucose > 0 ? new GlucoseMeasurement
                        {
                            GlucoseValue = (double)dto.CurrentGlucose,
                            Unit = unit,
                            Trend = trend,
                            Status = status,
                            MeasurementTime = dto.LastUpdated
                        } : null,
                        AverageGlucose = (double)dto.AverageGlucose,
                        LowestGlucose = (double)dto.LowestGlucose,
                        HighestGlucose = (double)dto.HighestGlucose,
                        TimeInRangePercentage = (double)dto.TimeInRangePercentage,
                        Unit = unit,
                        LastSyncTime = dto.LastUpdated,
                        SensorStatusText = "Active"
                    };
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiGlucoseService] GetDashboardSummaryAsync failed: {ex}");
        }
        
        return new GlucoseSummary(); // Empty state
    }

    public async Task<GlucoseMeasurement?> GetLatestReadingAsync()
    {
        var readings = await GetHistoryAsync("api/glucose/history?hours=24&page=1&pageSize=1");
        return readings.FirstOrDefault();
    }

    public async Task<IReadOnlyList<GlucoseMeasurement>> GetRecentReadingsAsync(TimeSpan timeSpan)
    {
        return await GetHistoryAsync($"api/glucose/history?hours={Math.Ceiling(timeSpan.TotalHours):0}");
    }

    public async Task<IReadOnlyList<GlucoseMeasurement>> GetReadingsForDateAsync(DateTime localDate)
    {
        var localDateOnly = localDate.Date;
        var localStart = new DateTime(localDateOnly.Year, localDateOnly.Month, localDateOnly.Day, 0, 0, 0, DateTimeKind.Local);
        var startUtc = localStart.ToUniversalTime();
        var endUtc = localStart.AddDays(1).ToUniversalTime();
        return await GetHistoryAsync(
            $"api/glucose/history?startUtc={Uri.EscapeDataString(startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"))}&endUtc={Uri.EscapeDataString(endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"))}");
    }

    public async Task<PagedGlucoseReadings> GetReadingsForDateAsync(DateTime localDate, int page, int pageSize)
    {
        var localDateOnly = localDate.Date;
        var localStart = new DateTime(localDateOnly.Year, localDateOnly.Month, localDateOnly.Day, 0, 0, 0, DateTimeKind.Local);
        var startUtc = localStart.ToUniversalTime();
        var endUtc = localStart.AddDays(1).ToUniversalTime();
        var path = $"api/glucose/history?startUtc={Uri.EscapeDataString(startUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"))}&endUtc={Uri.EscapeDataString(endUtc.ToString("yyyy-MM-ddTHH:mm:ssZ"))}&page={page}&pageSize={pageSize}";

        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, path);
            using var response = await _client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
                return new PagedGlucoseReadings([], page, pageSize, 0, 0);

            var result = await response.Content.ReadFromJsonAsync<PagedHistoryDto>();
            var items = MapHistory(result?.Items);
            return new PagedGlucoseReadings(items, result?.Page ?? page, result?.PageSize ?? pageSize,
                result?.TotalCount ?? 0, result?.TotalPages ?? 0);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiGlucoseService] Paged history failed: {ex}");
            return new PagedGlucoseReadings([], page, pageSize, 0, 0);
        }
    }

    private async Task<IReadOnlyList<GlucoseMeasurement>> GetHistoryAsync(string path)
    {
        try
        {
            using var request = await AuthorizedAsync(HttpMethod.Get, path);
            using var response = await _client.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<PagedHistoryDto>();
                return MapHistory(result?.Items);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[ApiGlucoseService] GetRecentReadingsAsync failed: {ex}");
        }
        return new List<GlucoseMeasurement>();
    }

    private static List<GlucoseMeasurement> MapHistory(IEnumerable<HistoryReadingDto>? readings) =>
        readings?
                    .Where(x => x.GlucoseValue.HasValue)
                    .Select(x => new GlucoseMeasurement
                    {
                        SequenceNumber = (ushort)Math.Clamp(x.SequenceNumber, ushort.MinValue, ushort.MaxValue),
                        GlucoseValue = (double)x.GlucoseValue!.Value,
                        Unit = Enum.TryParse<GlucoseUnit>(x.GlucoseUnit, true, out var unit) ? unit : GlucoseUnit.MgDl,
                        MeasurementTime = x.MeasurementTime.Kind == DateTimeKind.Utc ? x.MeasurementTime : DateTime.SpecifyKind(x.MeasurementTime, DateTimeKind.Utc),
                        Trend = Enum.TryParse<GlucoseTrend>(x.Trend, true, out var trend) ? trend : GlucoseTrend.Stable,
                        Status = Enum.TryParse<GlucoseStatus>(x.GlucoseStatus, true, out var status) ? status : GlucoseStatus.Normal
                    })
                    .ToList() ?? new List<GlucoseMeasurement>();

    public void NotifyReadingReceived(GlucoseMeasurement measurement)
    {
        GlucoseReadingReceived?.Invoke(this, measurement);
    }

    private sealed class HistoryReadingDto
    {
        public int SequenceNumber { get; set; }
        public decimal? GlucoseValue { get; set; }
        public string? GlucoseUnit { get; set; }
        public DateTime MeasurementTime { get; set; }
        public string? Trend { get; set; }
        public string? GlucoseStatus { get; set; }
    }

    private sealed class PagedHistoryDto
    {
        public List<HistoryReadingDto> Items { get; set; } = [];
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
    }

    private sealed class BackendSummaryDto
    {
        public decimal CurrentGlucose { get; set; }
        public string? GlucoseUnit { get; set; }
        public string? Trend { get; set; }
        public string? Status { get; set; }
        public decimal AverageGlucose { get; set; }
        public decimal LowestGlucose { get; set; }
        public decimal HighestGlucose { get; set; }
        public decimal TimeInRangePercentage { get; set; }
        public DateTime LastUpdated { get; set; }
    }
}
