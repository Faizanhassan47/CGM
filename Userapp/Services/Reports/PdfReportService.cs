using SkiaSharp;

namespace CGM.PatientApp.Services.Reports;

public interface IPdfReportService
{
    Task<string> GenerateReportPdfAsync(string patientName, string patientEmail, string dateRange,
        string avgGlucose, string timeInRange, string highestGlucose, string lowestGlucose);
}

public sealed class PdfReportService : IPdfReportService
{
    public async Task<string> GenerateReportPdfAsync(string patientName, string patientEmail, string dateRange,
        string avgGlucose, string timeInRange, string highestGlucose, string lowestGlucose)
    {
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"GlucoTrack_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        await Task.Run(() =>
        {
            using var stream = File.Create(filePath);
            using var document = SKDocument.CreatePdf(stream);
            const float width = 595;
            const float height = 842;
            using var canvas = document.BeginPage(width, height);

            using var teal = Paint(new SKColor(13, 148, 136));
            canvas.DrawRect(0, 0, width, 92, teal);
            using var title = Text(SKColors.White, 22, true);
            using var subtitle = Text(new SKColor(230, 244, 234), 11);
            canvas.DrawText("GlucoTrack Glucose Report", 28, 42, title);
            canvas.DrawText("Summary for discussion with your healthcare professional", 28, 66, subtitle);

            using var dark = Text(new SKColor(15, 23, 42), 12);
            using var bold = Text(new SKColor(15, 23, 42), 13, true);
            using var muted = Text(new SKColor(100, 116, 139), 10);
            using var card = Paint(new SKColor(248, 250, 249));
            using var border = Paint(new SKColor(226, 232, 240), SKPaintStyle.Stroke);
            var patientCard = new SKRect(28, 112, width - 28, 218);
            canvas.DrawRoundRect(patientCard, 10, 10, card);
            canvas.DrawRoundRect(patientCard, 10, 10, border);
            canvas.DrawText("PATIENT AND REPORT", 42, 139, bold);
            canvas.DrawText($"Name: {Safe(patientName)}", 42, 164, dark);
            canvas.DrawText($"Email: {Safe(patientEmail)}", 42, 184, muted);
            canvas.DrawText($"Period: {Safe(dateRange)}", 310, 164, dark);
            canvas.DrawText($"Generated: {DateTime.Now:g}", 310, 184, muted);

            canvas.DrawText("GLUCOSE SUMMARY", 28, 256, bold);
            var metrics = new[]
            {
                ("Average", avgGlucose, new SKColor(13, 148, 136)),
                ("Time in range", timeInRange, new SKColor(5, 150, 105)),
                ("Highest", highestGlucose, new SKColor(220, 38, 38)),
                ("Lowest", lowestGlucose, new SKColor(37, 99, 235))
            };
            const float gap = 12;
            var metricWidth = (width - 56 - gap * 3) / 4;
            for (var i = 0; i < metrics.Length; i++)
                DrawMetric(canvas, 28 + i * (metricWidth + gap), 272, metricWidth, metrics[i].Item1, metrics[i].Item2, metrics[i].Item3);

            canvas.DrawText("IMPORTANT", 28, 390, bold);
            canvas.DrawText("This report contains app-calculated summary values for the selected period.", 28, 418, dark);
            canvas.DrawText("Missing or stale sensor readings can affect completeness. Verify unexpected results in the app.", 28, 440, muted);
            canvas.DrawText("Do not change medication, insulin, diet, or treatment based only on this report.", 28, 462, muted);

            using var line = Paint(new SKColor(203, 213, 225), SKPaintStyle.Stroke);
            canvas.DrawLine(28, height - 62, width - 28, height - 62, line);
            using var footer = Text(new SKColor(100, 116, 139), 9);
            canvas.DrawText("Confidential health information - GlucoTrack Patient Platform", 28, height - 44, footer);
            canvas.DrawText("Discuss glucose readings and treatment decisions with a qualified healthcare professional.", 28, height - 30, footer);

            document.EndPage();
            document.Close();
        });
        return filePath;
    }

    private static void DrawMetric(SKCanvas canvas, float x, float y, float width, string label, string value, SKColor accent)
    {
        using var background = Paint(new SKColor(248, 250, 249));
        using var border = Paint(new SKColor(226, 232, 240), SKPaintStyle.Stroke);
        var rect = new SKRect(x, y, x + width, y + 78);
        canvas.DrawRoundRect(rect, 8, 8, background);
        canvas.DrawRoundRect(rect, 8, 8, border);
        using var labelPaint = Text(new SKColor(100, 116, 139), 10);
        using var valuePaint = Text(accent, 15, true);
        canvas.DrawText(label, x + 9, y + 25, labelPaint);
        canvas.DrawText(Safe(value), x + 9, y + 53, valuePaint);
    }

    private static SKPaint Paint(SKColor color, SKPaintStyle style = SKPaintStyle.Fill) =>
        new() { Color = color, Style = style, StrokeWidth = 1, IsAntialias = true };

    private static SKPaint Text(SKColor color, float size, bool bold = false) =>
        new() { Color = color, TextSize = size, IsAntialias = true, FakeBoldText = bold };

    private static string Safe(string? value) => string.IsNullOrWhiteSpace(value) ? "Not available" : value.Trim();
}
