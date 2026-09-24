using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CGM.PatientApp.Interfaces;
using CGM.PatientApp.Models;

namespace CGM.PatientApp.Services.Reports;

public sealed class ApiReportService : IReportService
{
    private readonly HttpClient _client;
    private readonly IPdfReportService _pdfReportService;

    public ApiReportService(HttpClient client, IPdfReportService pdfReportService)
    {
        _client = client;
        _pdfReportService = pdfReportService;
    }

    public async Task<PdfReportSummary> GetDetailedReportAsync(int? targetUserId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var start = startDate?.Date ?? DateTime.UtcNow.Date.AddDays(-6);
        var end = endDate?.Date ?? DateTime.UtcNow.Date;

        var startEncoded = Uri.EscapeDataString(start.ToString("yyyy-MM-dd"));
        var endEncoded = Uri.EscapeDataString(end.ToString("yyyy-MM-dd"));

        var url = $"api/reports/detailed?startDate={startEncoded}&endDate={endEncoded}";
        if (targetUserId.HasValue && targetUserId.Value > 0)
        {
            url += $"&targetUserId={targetUserId.Value}";
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        var token = await SecureStorage.Default.GetAsync("cgm_access_token");
        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using var response = await _client.SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            var errorJson = await response.Content.ReadAsStringAsync();
            try
            {
                var doc = JsonDocument.Parse(errorJson);
                if (doc.RootElement.TryGetProperty("message", out var msg))
                {
                    throw new InvalidOperationException(msg.GetString());
                }
            }
            catch
            {
                // ignore
            }
            throw new InvalidOperationException($"Unable to fetch report from server (Status: {response.StatusCode})");
        }

        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var summary = await response.Content.ReadFromJsonAsync<PdfReportSummary>(options);
        return summary ?? new PdfReportSummary();
    }

    public async Task<string> ExportReportPdfAsync(DateTime startDate, DateTime endDate, int? targetUserId = null)
    {
        var summary = await GetDetailedReportAsync(targetUserId, startDate, endDate);
        return await _pdfReportService.GenerateReportPdfAsync(summary);
    }

    public async Task ShareReportAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            throw new FileNotFoundException("Report file does not exist.", filePath);

        await Share.Default.RequestAsync(new ShareFileRequest
        {
            Title = "GlucoTrack Clinical AGP Report",
            File = new ShareFile(filePath)
        });
    }
}
