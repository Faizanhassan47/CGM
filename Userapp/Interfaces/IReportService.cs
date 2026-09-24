using CGM.PatientApp.Models;

namespace CGM.PatientApp.Interfaces;

public interface IReportService
{
    Task<PdfReportSummary> GetDetailedReportAsync(int? targetUserId = null, DateTime? startDate = null, DateTime? endDate = null);
    Task<string> ExportReportPdfAsync(DateTime startDate, DateTime endDate, int? targetUserId = null);
    Task ShareReportAsync(string filePath);
}
