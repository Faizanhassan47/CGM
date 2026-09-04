using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IReportService
{
    Task<ReportSummary> GetReportSummaryAsync(DateTime startDate, DateTime endDate);
    Task<string> ExportReportPdfAsync(DateTime startDate, DateTime endDate);
    Task ShareReportAsync(string filePath);
}
