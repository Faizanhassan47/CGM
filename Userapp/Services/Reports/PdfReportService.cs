using CGM.PatientApp.Interfaces;
using SkiaSharp;

namespace CGM.PatientApp.Services.Reports;

public interface IPdfReportService
{
    Task<string> GenerateReportPdfAsync(PdfReportSummary summary);
    Task<string> GenerateReportPdfAsync(string patientName, string patientEmail, string dateRange,
        string avgGlucose, string timeInRange, string highestGlucose, string lowestGlucose,
        string estimatedA1c, string glucoseVariability);
}

public sealed class PdfReportService : IPdfReportService
{
    public Task<string> GenerateReportPdfAsync(string patientName, string patientEmail, string dateRange,
        string avgGlucose, string timeInRange, string highestGlucose, string lowestGlucose,
        string estimatedA1c, string glucoseVariability)
    {
        var summary = new PdfReportSummary
        {
            PatientName = patientName,
            PatientEmail = patientEmail,
            DateRange = dateRange,
            AvgGlucose = avgGlucose,
            TimeInRange = timeInRange,
            HighestGlucose = highestGlucose,
            LowestGlucose = lowestGlucose,
            EstimatedA1c = estimatedA1c,
            GlucoseVariability = glucoseVariability,
            GeneratedAt = DateTime.UtcNow
        };

        return GenerateReportPdfAsync(summary);
    }

    public async Task<string> GenerateReportPdfAsync(PdfReportSummary summary)
    {
        var filePath = Path.Combine(FileSystem.CacheDirectory, $"GlucoTrack_Report_{DateTime.Now:yyyyMMdd_HHmmss}.pdf");

        await Task.Run(() =>
        {
            using var stream = File.Create(filePath);
            using var document = SKDocument.CreatePdf(stream);
            const float width = 595;
            const float height = 842;
            using var canvas = document.BeginPage(width, height);

            // Purple & Neutral Palette
            var primaryPurple = new SKColor(109, 40, 217);    // #6D28D9
            var darkPurple = new SKColor(76, 29, 149);        // #4C1D95
            var lightPurple = new SKColor(245, 243, 255);     // #F5F3FF
            var borderPurple = new SKColor(221, 214, 254);    // #DDD6FE
            var textDark = new SKColor(15, 23, 42);           // #0F172A
            var textMuted = new SKColor(100, 116, 139);       // #64748B
            var cardBg = new SKColor(248, 250, 252);          // #F8FAFC
            var cardBorder = new SKColor(226, 232, 240);      // #E2E8F0
            var emerald = new SKColor(16, 185, 129);          // #10B981
            var amber = new SKColor(245, 158, 11);            // #F59E0B
            var rose = new SKColor(239, 68, 68);              // #EF4444

            const float leftMargin = 28f;
            const float rightMargin = width - 28f;
            const float contentWidth = rightMargin - leftMargin;

            // 1. TOP HEADER BANNER (Gradient Purple)
            using (var headerShader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0),
                new SKPoint(width, 84),
                new[] { darkPurple, primaryPurple },
                new[] { 0f, 1f },
                SKShaderTileMode.Clamp))
            using (var headerPaint = new SKPaint { Shader = headerShader, IsAntialias = true })
            {
                canvas.DrawRect(0, 0, width, 84, headerPaint);
            }

            // Decorative background circle in header
            using (var circlePaint = Paint(new SKColor(255, 255, 255, 18)))
            {
                canvas.DrawCircle(width - 35, 18, 65, circlePaint);
            }

            using (var titlePaint = Text(SKColors.White, 19, true))
            using (var subPaint = Text(new SKColor(233, 213, 255), 9.5f))
            {
                canvas.DrawText("GlucoTrack™ Glucose Report", leftMargin, 36, titlePaint);
                canvas.DrawText("Continuous Interstitial Sensor Readings & Glycemic Log", leftMargin, 58, subPaint);
            }

            // Header Right Badge
            var badgeRect = new SKRect(width - 145, 28, width - 28, 54);
            using (var badgeBg = Paint(new SKColor(255, 255, 255, 30)))
            using (var badgeBorder = Paint(new SKColor(255, 255, 255, 70), SKPaintStyle.Stroke))
            using (var badgeText = Text(SKColors.White, 8.5f, true))
            {
                canvas.DrawRoundRect(badgeRect, 13, 13, badgeBg);
                canvas.DrawRoundRect(badgeRect, 13, 13, badgeBorder);
                canvas.DrawText("DATABASE EXPORT", width - 135, 45, badgeText);
            }

            // 2. PATIENT & REPORT META CARD
            var metaRect = new SKRect(leftMargin, 96, rightMargin, 160);
            using (var metaBg = Paint(cardBg))
            using (var metaStroke = Paint(borderPurple, SKPaintStyle.Stroke))
            {
                canvas.DrawRoundRect(metaRect, 8, 8, metaBg);
                canvas.DrawRoundRect(metaRect, 8, 8, metaStroke);
            }

            using (var tagPaint = Text(primaryPurple, 8, true))
            using (var darkBold = Text(textDark, 11f, true))
            using (var mutedPaint = Text(textMuted, 8.5f))
            {
                // Left column: Patient
                canvas.DrawText("PATIENT PROFILE", leftMargin + 14, 114, tagPaint);
                canvas.DrawText(Safe(summary.PatientName, "Patient"), leftMargin + 14, 132, darkBold);
                canvas.DrawText(Safe(summary.PatientEmail, "Patient Account"), leftMargin + 14, 148, mutedPaint);

                // Right column: Period
                canvas.DrawText("REPORTING PERIOD", 320, 114, tagPaint);
                canvas.DrawText(Safe(summary.DateRange, $"{DateTime.Now:MMM dd, yyyy}"), 320, 132, darkBold);
                var totalPts = summary.TotalReadings > 0 ? summary.TotalReadings : summary.RecentReadings.Count;
                var recordCountStr = totalPts > 0 ? $"{totalPts} records" : "Live Log";
                canvas.DrawText($"Generated: {summary.GeneratedAt.ToLocalTime():dd/MM/yyyy HH:mm}  |  {recordCountStr}", 320, 148, mutedPaint);
            }

            // 3. TOP 3 METRICS: AVERAGE, HIGHEST, LOWEST
            using (var secTitle = Text(textDark, 10.5f, true))
            {
                canvas.DrawText("GLUCOSE SUMMARY", leftMargin, 180, secTitle);
            }

            const float cardGap = 10f;
            var metricWidth = (contentWidth - cardGap * 2) / 3;
            var cardY = 192f;
            var cardH = 56f;

            // 1) Average
            DrawSummaryMetricCard(canvas, leftMargin, cardY, metricWidth, cardH,
                "Average Glucose", summary.AvgGlucose, "Target: 70–140 mg/dL", primaryPurple);

            // 2) Highest
            DrawSummaryMetricCard(canvas, leftMargin + metricWidth + cardGap, cardY, metricWidth, cardH,
                "Highest Reading", summary.HighestGlucose, "Maximum value recorded", amber);

            // 3) Lowest
            DrawSummaryMetricCard(canvas, leftMargin + (metricWidth + cardGap) * 2, cardY, metricWidth, cardH,
                "Lowest Reading", summary.LowestGlucose, "Minimum value recorded", rose);

            // 4. MAIN TABLE: DATABASE MEASUREMENT RECORDS
            var tableY = 266f;
            using (var secTitle = Text(textDark, 10.5f, true))
            {
                canvas.DrawText("DATABASE MEASUREMENTS LOG", leftMargin, tableY, secTitle);
            }

            var tableTop = tableY + 8f;
            var tableBottom = 728f;
            var tableRect = new SKRect(leftMargin, tableTop, rightMargin, tableBottom);

            using (var tblBg = Paint(SKColors.White))
            using (var tblStroke = Paint(cardBorder, SKPaintStyle.Stroke))
            {
                canvas.DrawRoundRect(tableRect, 8, 8, tblBg);
                canvas.DrawRoundRect(tableRect, 8, 8, tblStroke);
            }

            // Header row
            const float thH = 24f;
            var thRect = new SKRect(leftMargin, tableTop, rightMargin, tableTop + thH);
            using (var thBg = Paint(lightPurple))
            {
                canvas.DrawRoundRect(thRect, 8, 8, thBg);
                canvas.DrawRect(leftMargin, tableTop + 12, contentWidth, 12, thBg); // flatten bottom corners
            }

            using (var thText = Text(darkPurple, 8.5f, true))
            {
                canvas.DrawText("#", leftMargin + 10, tableTop + 16, thText);
                canvas.DrawText("Date & Time", leftMargin + 36, tableTop + 16, thText);
                canvas.DrawText("Glucose Value", leftMargin + 185, tableTop + 16, thText);
                canvas.DrawText("Status", leftMargin + 295, tableTop + 16, thText);
                canvas.DrawText("Clinical Reference Range", leftMargin + 380, tableTop + 16, thText);
            }

            // Data rows
            var readings = summary.RecentReadings;
            var rowStartY = tableTop + thH;
            const float rowH = 21.5f;
            const int maxRows = 19;

            if (readings == null || readings.Count == 0)
            {
                using var emptyText = Text(textMuted, 9f);
                canvas.DrawText("No glucose records available for the selected period.", leftMargin + 20, rowStartY + 25, emptyText);
            }
            else
            {
                var countToRender = Math.Min(maxRows, readings.Count);
                for (var r = 0; r < countToRender; r++)
                {
                    var item = readings[r];
                    var curY = rowStartY + r * rowH;

                    // Alternating row background
                    if (r % 2 == 1)
                    {
                        using var rowBg = Paint(cardBg);
                        canvas.DrawRect(leftMargin + 1, curY, contentWidth - 2, rowH, rowBg);
                    }

                    // Divider line between rows
                    using (var rLine = Paint(new SKColor(241, 245, 249), SKPaintStyle.Stroke))
                    {
                        canvas.DrawLine(leftMargin, curY + rowH, rightMargin, curY + rowH, rLine);
                    }

                    using var rText = Text(textDark, 8.5f);
                    using var rMuted = Text(textMuted, 8f);
                    using var rBold = Text(textDark, 9f, true);

                    // Row index
                    canvas.DrawText($"{(r + 1):D2}", leftMargin + 10, curY + 14.5f, rMuted);

                    // Date & Time
                    canvas.DrawText(Safe(item.TimeFormatted, item.Time.ToString("dd MMM yyyy HH:mm")), leftMargin + 36, curY + 14.5f, rText);

                    // Glucose Value
                    var valColor = item.Value < 70 ? rose : (item.Value > 180 ? amber : textDark);
                    using var valPaint = Text(valColor, 9.5f, true);
                    canvas.DrawText($"{item.Value:0} mg/dL", leftMargin + 185, curY + 14.5f, valPaint);

                    // Status Pill
                    var status = item.Status;
                    if (string.IsNullOrWhiteSpace(status))
                    {
                        status = item.Value < 70 ? "Low" : (item.Value > 180 ? "High" : "Normal");
                    }
                    var statusColor = status.Contains("Low") ? rose : (status.Contains("High") ? amber : emerald);
                    DrawStatusPill(canvas, leftMargin + 295, curY + 3.5f, 65, 14.5f, status, statusColor);

                    // Reference Range Note
                    var refNote = item.Value < 70 ? "Below Target (< 70 mg/dL)" : (item.Value > 180 ? "Above Target (> 180 mg/dL)" : "In Target (70–180 mg/dL)");
                    using var refPaint = Text(textMuted, 8f);
                    canvas.DrawText(refNote, leftMargin + 380, curY + 14.5f, refPaint);
                }
            }

            // 5. NOTICE BOX
            var noteY = 738f;
            var noteRect = new SKRect(leftMargin, noteY, rightMargin, noteY + 36);
            using (var noteBg = Paint(lightPurple))
            using (var noteStroke = Paint(borderPurple, SKPaintStyle.Stroke))
            using (var noteAccent = Paint(primaryPurple))
            {
                canvas.DrawRoundRect(noteRect, 6, 6, noteBg);
                canvas.DrawRoundRect(noteRect, 6, 6, noteStroke);
                canvas.DrawRoundRect(new SKRect(leftMargin, noteY, leftMargin + 3.5f, noteY + 36), 2, 2, noteAccent);
            }

            using (var noteTitle = Text(primaryPurple, 8f, true))
            using (var noteBody = Text(textMuted, 7.5f))
            {
                canvas.DrawText("IMPORTANT CLINICAL NOTICE", leftMargin + 12, noteY + 13, noteTitle);
                canvas.DrawText("Values represent recorded continuous glucose measurements from the database. Do not modify medication or insulin doses without professional consultation.", leftMargin + 12, noteY + 26, noteBody);
            }

            // 6. FOOTER
            using (var linePaint = Paint(cardBorder, SKPaintStyle.Stroke))
            {
                canvas.DrawLine(leftMargin, height - 34, rightMargin, height - 34, linePaint);
            }

            using (var footText = Text(textMuted, 8f))
            using (var pageText = Text(textMuted, 8f, true))
            {
                canvas.DrawText("GlucoTrack™ Patient Platform  •  Database Export Report  •  Confidential Health Information", leftMargin, height - 20, footText);
                canvas.DrawText("Page 1 of 1", width - 80, height - 20, pageText);
            }

            document.EndPage();
            document.Close();
        });

        return filePath;
    }

    private static void DrawSummaryMetricCard(SKCanvas canvas, float x, float y, float width, float height, string label, string value, string subtext, SKColor accent)
    {
        var rect = new SKRect(x, y, x + width, y + height);
        using var background = Paint(new SKColor(248, 250, 252));
        using var border = Paint(new SKColor(226, 232, 240), SKPaintStyle.Stroke);
        canvas.DrawRoundRect(rect, 6, 6, background);
        canvas.DrawRoundRect(rect, 6, 6, border);

        // Accent top bar
        using var barPaint = Paint(accent);
        canvas.DrawRoundRect(new SKRect(x + 10, y, x + 42, y + 2.5f), 1, 1, barPaint);

        using var labelPaint = Text(new SKColor(100, 116, 139), 8.5f);
        using var valuePaint = Text(accent, 15f, true);
        using var subPaint = Text(new SKColor(148, 163, 184), 7.5f);

        canvas.DrawText(label, x + 10, y + 17, labelPaint);
        canvas.DrawText(Safe(value, "-"), x + 10, y + 36, valuePaint);
        canvas.DrawText(subtext, x + 10, y + 48, subPaint);
    }

    private static void DrawStatusPill(SKCanvas canvas, float x, float y, float width, float height, string text, SKColor color)
    {
        var rect = new SKRect(x, y, x + width, y + height);
        using var fill = Paint(new SKColor(color.Red, color.Green, color.Blue, 28));
        using var stroke = Paint(new SKColor(color.Red, color.Green, color.Blue, 110), SKPaintStyle.Stroke);
        using var txt = Text(color, 7.5f, true);

        canvas.DrawRoundRect(rect, height / 2, height / 2, fill);
        canvas.DrawRoundRect(rect, height / 2, height / 2, stroke);
        canvas.DrawText(text, x + 7, y + height - 3.5f, txt);
    }

    private static SKPaint Paint(SKColor color, SKPaintStyle style = SKPaintStyle.Fill) =>
        new() { Color = color, Style = style, StrokeWidth = 1, IsAntialias = true };

    private static SKPaint Text(SKColor color, float size, bool bold = false) =>
        new() { Color = color, TextSize = size, IsAntialias = true, FakeBoldText = bold };

    private static string Safe(string? value, string fallback = "Not available") =>
        string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
}
