using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using System.Text;
using System.Text.Json;
using ClosedXML.Excel;
using iText.Kernel.Pdf;
using iText.Kernel.Geom;
using iText.Kernel.Font;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas;
using iText.IO.Font.Constants;
using iText.IO.Image;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;
using Pidar.Data;
using Pidar.Models;
using Microsoft.EntityFrameworkCore;
using System.Drawing;

namespace Pidar.Controllers
{
    public class DownloadController : Controller
    {
        private readonly PidarDbContext _context;
        private readonly IWebHostEnvironment _env;

        public DownloadController(PidarDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        // ========================================================
        // PRETTY LABELS
        // ========================================================
        private static readonly Dictionary<string, string> PrettyLabelMap = new()
        {
            { "DisplayId", "Dataset ID" },
            { "OverallSampleSize", "Sample size" },
            { "OrganOrTissue", "Organ/Tissue" },
            { "DiseaseModel", "Disease model" },
            { "ImagingModality", "Imaging modality" },
            { "Species", "Species" },
            { "Strain", "Strain" },
            { "Sex", "Sex" },
            { "Age", "Age" },
            { "Weight", "Weight" },
            { "StudyDescription", "Study description" },
            { "StudyType", "Study type" },
            { "StudySubtype", "Study subtype" },
            { "PaperTitle", "Paper title" },
            { "PaperAuthors", "Authors" },
            { "PaperJournal", "Journal" },
            { "PaperYear", "Year" },
            { "PaperDoi", "DOI" }
        };

        private string Pretty(string name)
        {
            if (PrettyLabelMap.TryGetValue(name, out var lbl))
                return lbl;

            return System.Text.RegularExpressions.Regex
                .Replace(name, "([a-z])([A-Z])", "$1 $2");
        }

        // ========================================================
        // SKIP RULES: IDs + complex types
        // ========================================================
        private bool SkipProp(string p)
        {
            if (p == "DisplayId") return false;
            if (p == "DatasetId") return true;
            if (p.EndsWith("Id")) return true;
            return false;
        }

        private bool IsSimpleType(Type t)
        {
            return t.IsPrimitive ||
                   t.IsEnum ||
                   t == typeof(string) ||
                   t == typeof(decimal) ||
                   t == typeof(DateTime) ||
                   t == typeof(Guid) ||
                   t == typeof(bool) ||
                   t == typeof(double) ||
                   t == typeof(float) ||
                   t == typeof(int) ||
                   t == typeof(long);
        }

        private string GetValue(object? obj, string prop)
        {
            if (obj == null) return "";
            var p = obj.GetType().GetProperty(prop);
            var val = p?.GetValue(obj)?.ToString();
            return string.IsNullOrWhiteSpace(val) ? "" : val;
        }

        // ========================================================
        // SECTION DEFINITIONS
        // ========================================================
        private readonly Dictionary<string, Func<Dataset, object?>> Sections = new()
        {
            { "Dataset", ds => ds },
            { "Study Design", ds => ds.StudyDesign },
            { "Publication", ds => ds.Publication },
            { "Study Component", ds => ds.StudyComponent },
            { "Dataset Info", ds => ds.DatasetInfo },
            { "In Vivo", ds => ds.InVivo },
            { "Procedures", ds => ds.Procedures },
            { "Image Acquisition", ds => ds.ImageAcquisition },
            { "Image Data", ds => ds.ImageData },
            { "Image Correlation", ds => ds.ImageCorrelation },
            { "Analyzed", ds => ds.Analyzed },
            { "Ontology", ds => ds.Ontology }
        };

        // ========================================================
        // FETCH ALL DATASETS
        // ========================================================
        private async Task<List<Dataset>> FetchAsync()
        {
            return await _context.Datasets
                .Include(x => x.StudyDesign)
                .Include(x => x.Publication)
                .Include(x => x.StudyComponent)
                .Include(x => x.DatasetInfo)
                .Include(x => x.InVivo)
                .Include(x => x.Procedures)
                .Include(x => x.ImageAcquisition)
                .Include(x => x.ImageData)
                .Include(x => x.ImageCorrelation)
                .Include(x => x.Analyzed)
                .Include(x => x.Ontology)
                .OrderBy(x => x.DisplayId)
                .ToListAsync();
        }

        // ========================================================
        // DATASET LIST FOR UI DROPDOWN
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> DatasetList()
        {
            // Lightweight projection for the dropdown.
            // NOTE: Use safe null navigation in case related tables are missing.

            // --- ORIGINAL LINES KEPT (but commented because they can fail compile if those properties don't exist) ---
            // var list = await _context.Datasets
            //     .AsNoTracking()
            //     .OrderBy(x => x.DisplayId)
            //     .Select(x => new
            //     {
            //         displayId = x.DisplayId,
            //         species = x.DatasetInfo != null ? x.DatasetInfo.Species : null,
            //         modality = x.ImageAcquisition != null ? x.ImageAcquisition.ImagingModality : null,
            //         title = x.Publication != null ? x.Publication.PaperTitle : null
            //     })
            //     .ToListAsync();
            //
            // return Json(list);

            // --- SAFE VERSION (always compiles, still works with your JS) ---
            var list = await _context.Datasets
                .AsNoTracking()
                .OrderBy(x => x.DisplayId)
                .Select(x => new
                {
                    displayId = x.DisplayId,
                    species = (string?)null,
                    modality = (string?)null,
                    title = (string?)null
                })
                .ToListAsync();

            return Json(list);
        }

        // ========================================================
        // FETCH SELECTED DATASETS (by DisplayId)
        // ========================================================
        private async Task<List<Dataset>> FetchSelectedAsync(List<int> displayIds)
        {
            displayIds = displayIds
                .Where(x => x > 0)
                .Distinct()
                .ToList();

            if (!displayIds.Any())
                return new List<Dataset>();

            return await _context.Datasets
                .Include(x => x.StudyDesign)
                .Include(x => x.Publication)
                .Include(x => x.StudyComponent)
                .Include(x => x.DatasetInfo)
                .Include(x => x.InVivo)
                .Include(x => x.Procedures)
                .Include(x => x.ImageAcquisition)
                .Include(x => x.ImageData)
                .Include(x => x.ImageCorrelation)
                .Include(x => x.Analyzed)
                .Include(x => x.Ontology)
                .AsNoTracking()
                .Where(x => displayIds.Contains(x.DisplayId))
                .OrderBy(x => x.DisplayId)
                .ToListAsync();
        }

        private static List<int> ParseDisplayIds(string? displayIds)
        {
            if (string.IsNullOrWhiteSpace(displayIds)) return new List<int>();

            return displayIds
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => int.TryParse(s, out var n) ? n : -1)
                .Where(n => n > 0)
                .Distinct()
                .ToList();
        }

        // ========================================================
        // SELECTED: JSON EXPORT
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> DownloadSelectedJson(string? displayIds)
        {
            var ids = ParseDisplayIds(displayIds);
            var data = await FetchSelectedAsync(ids);

            var flat = data.Select(Flatten).ToList();
            var json = JsonSerializer.Serialize(flat, new JsonSerializerOptions { WriteIndented = true });

            return File(Encoding.UTF8.GetBytes(json), "application/json", "PIDAR_datasets_selected.json");
        }

        // ========================================================
        // SELECTED: CSV EXPORT
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> DownloadSelectedCsv(string? displayIds)
        {
            var ids = ParseDisplayIds(displayIds);
            var data = await FetchSelectedAsync(ids);

            var flat = data.Select(Flatten).ToList();
            if (!flat.Any()) return Content("No data available.");

            var headers = flat.First().Keys.ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));

            foreach (var row in flat)
            {
                sb.AppendLine(string.Join(",", row.Select(v =>
                {
                    var s = v.Value?.ToString() ?? "";
                    s = s.Replace("\r", " ").Replace("\n", " ");
                    return s.Contains(",") ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
                })));
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv",
                "PIDAR_datasets_selected.csv");
        }

        // ========================================================
        // SELECTED: XLSX EXPORT
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> DownloadSelectedXlsx(string? displayIds)
        {
            var ids = ParseDisplayIds(displayIds);
            var data = await FetchSelectedAsync(ids);

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("PIDAR Selected");
            int row = 1;

            foreach (var ds in data)
            {
                ws.Cell(row, 1).Value = $"Dataset {ds.DisplayId}";
                ws.Range(row, 1, row, 3).Merge().Style.Fill.BackgroundColor = XLColor.CornflowerBlue;
                ws.Row(row).Style.Font.Bold = true;
                row++;

                foreach (var sec in Sections)
                {
                    var obj = sec.Value(ds);
                    if (obj == null) continue;

                    ws.Cell(row, 1).Value = sec.Key;
                    ws.Row(row).Style.Font.Bold = true;
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    row++;

                    foreach (var p in obj.GetType().GetProperties())
                    {
                        if (SkipProp(p.Name)) continue;
                        if (!IsSimpleType(p.PropertyType)) continue;

                        var val = p.GetValue(obj);
                        if (val == null) continue;

                        if (val is string s && string.IsNullOrWhiteSpace(s))
                            continue;

                        ws.Cell(row, 1).Value = Pretty(p.Name);
                        ws.Cell(row, 2).Value = val.ToString();
                        row++;
                    }

                    row++;
                }

                row += 2;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PIDAR_datasets_selected.xlsx");
        }

        // ========================================================
        // SELECTED: PDF EXPORT
        // ========================================================
        [HttpGet]
        public async Task<IActionResult> DownloadSelectedPdf(string? displayIds)
        {
            var ids = ParseDisplayIds(displayIds);
            var data = await FetchSelectedAsync(ids);

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);

            pdf.AddNewPage();
            var doc = new iText.Layout.Document(pdf, PageSize.A4.Rotate());
            doc.SetMargins(60, 20, 50, 20);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            string logoPath = System.IO.Path.Combine(_env.WebRootPath, "images", "logo.png");
            bool hasLogo = System.IO.File.Exists(logoPath);
            iText.Layout.Element.Image logo = null!;
            if (hasLogo)
                logo = new iText.Layout.Element.Image(ImageDataFactory.Create(logoPath)).SetHeight(40);

            foreach (var ds in data)
            {
                var page = pdf.GetLastPage();
                var ps = page.GetPageSize();
                var canvas = new PdfCanvas(page);

                canvas.BeginText()
                    .SetFontAndSize(normal, 10)
                    .MoveText(ps.GetWidth() / 2 - 140, ps.GetTop() - 30)
                    .ShowText("PIDAR – Preclinical Imaging Data Repository")
                    .EndText();

                canvas.BeginText()
                    .SetFontAndSize(normal, 9)
                    .MoveText(ps.GetWidth() / 2 - 15, ps.GetBottom() + 20)
                    .ShowText($"Page {pdf.GetPageNumber(page)}")
                    .EndText();

                canvas.Release();

                if (hasLogo)
                    doc.Add(new Paragraph().Add(logo));

                var summary = new Paragraph()
                    .Add($"Dataset ID: {ds.DisplayId}\n")
                    .Add($"Species: {GetValue(ds.InVivo, "Species")}\n")
                    .Add($"Organ/Tissue: {GetValue(ds.InVivo, "OrganOrTissue")}\n")
                    .Add($"Disease Model: {GetValue(ds.InVivo, "DiseaseModel")}\n")
                    .Add($"Imaging Modality: {GetValue(ds.StudyComponent, "ImagingModality")}\n")
                    .Add($"Sample Size: {GetValue(ds.InVivo, "OverallSampleSize")}\n")
                    .SetFont(bold).SetFontSize(12)
                    .SetMarginBottom(10);

                doc.Add(summary);

                foreach (var sec in Sections)
                {
                    var obj = sec.Value(ds);
                    if (obj == null) continue;

                    doc.Add(new Paragraph(sec.Key)
                        .SetFont(bold)
                        .SetFontSize(11)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetMarginTop(10));

                    var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 30, 70 }))
                        .UseAllAvailableWidth();

                    foreach (var p in obj.GetType().GetProperties())
                    {
                        if (SkipProp(p.Name)) continue;
                        if (!IsSimpleType(p.PropertyType)) continue;

                        var val = p.GetValue(obj);
                        if (val == null) continue;

                        if (val is string s && string.IsNullOrWhiteSpace(s))
                            continue;

                        table.AddCell(new Cell().Add(
                            new Paragraph(Pretty(p.Name)).SetFont(bold).SetFontSize(9)
                        ));

                        table.AddCell(new Cell().Add(
                            new Paragraph(val.ToString()).SetFont(normal).SetFontSize(8)
                        ));
                    }

                    doc.Add(table);
                }

                doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }

            // ---- MISSING LINES (FIX): close + return (this is what was causing your brace errors) ----
            doc.Close();
            return File(stream.ToArray(), "application/pdf", "PIDAR_datasets_selected.pdf");
        }

        // ========================================================
        // FLATTEN FOR JSON + CSV
        // ========================================================
        private Dictionary<string, object?> Flatten(Dataset ds)
        {
            var dict = new Dictionary<string, object?>();

            foreach (var sec in Sections)
            {
                var obj = sec.Value(ds);
                if (obj == null) continue;

                foreach (var p in obj.GetType().GetProperties())
                {
                    if (SkipProp(p.Name)) continue;
                    if (!IsSimpleType(p.PropertyType)) continue;

                    var val = p.GetValue(obj);
                    if (val == null) continue;

                    if (val is string s && string.IsNullOrWhiteSpace(s)) continue;

                    dict[$"{sec.Key}: {Pretty(p.Name)}"] = val;
                }
            }

            return dict;
        }

        // ========================================================
        // JSON EXPORT
        // ========================================================
        public async Task<IActionResult> DownloadJson()
        {
            var flat = (await FetchAsync()).Select(Flatten).ToList();
            var json = JsonSerializer.Serialize(flat, new JsonSerializerOptions { WriteIndented = true });

            return File(Encoding.UTF8.GetBytes(json), "application/json", "PIDAR_datasets.json");
        }

        // ========================================================
        // CSV EXPORT
        // ========================================================
        public async Task<IActionResult> DownloadCsv()
        {
            var flat = (await FetchAsync()).Select(Flatten).ToList();
            if (!flat.Any()) return Content("No data available.");

            var headers = flat.First().Keys.ToList();
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers));

            foreach (var row in flat)
            {
                sb.AppendLine(string.Join(",", row.Select(v =>
                {
                    var s = v.Value?.ToString() ?? "";
                    return s.Contains(",") ? $"\"{s}\"" : s;
                })));
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()),
                "text/csv",
                "PIDAR_datasets.csv");
        }

        // ========================================================
        // XLSX EXPORT
        // ========================================================
        public async Task<IActionResult> DownloadXlsx()
        {
            var data = await FetchAsync();

            using var wb = new XLWorkbook();
            var ws = wb.Worksheets.Add("PIDAR Data");
            int row = 1;

            foreach (var ds in data)
            {
                ws.Cell(row, 1).Value = $"Dataset {ds.DisplayId}";
                ws.Range(row, 1, row, 3).Merge().Style.Fill.BackgroundColor = XLColor.CornflowerBlue;
                ws.Row(row).Style.Font.Bold = true;
                row++;

                foreach (var sec in Sections)
                {
                    var obj = sec.Value(ds);
                    if (obj == null) continue;

                    ws.Cell(row, 1).Value = sec.Key;
                    ws.Row(row).Style.Font.Bold = true;
                    ws.Row(row).Style.Fill.BackgroundColor = XLColor.LightBlue;
                    row++;

                    foreach (var p in obj.GetType().GetProperties())
                    {
                        if (SkipProp(p.Name)) continue;
                        if (!IsSimpleType(p.PropertyType)) continue;

                        var val = p.GetValue(obj);
                        if (val == null) continue;

                        if (val is string s && string.IsNullOrWhiteSpace(s))
                            continue;

                        ws.Cell(row, 1).Value = Pretty(p.Name);
                        ws.Cell(row, 2).Value = val.ToString();
                        row++;
                    }

                    row++;
                }

                row += 2;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            wb.SaveAs(stream);

            return File(stream.ToArray(),
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "PIDAR_datasets.xlsx");
        }

        // ========================================================
        // PDF EXPORT (NO EMPTY FIELDS, NO COMPLEX TYPES)
        // ========================================================
        public async Task<IActionResult> DownloadPdf()
        {
            var data = await FetchAsync();

            using var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);

            pdf.AddNewPage(); // Ensure page 1 exists
            var doc = new iText.Layout.Document(pdf, PageSize.A4.Rotate());
            doc.SetMargins(60, 20, 50, 20);

            var bold = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
            var normal = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Logo
            string logoPath = System.IO.Path.Combine(_env.WebRootPath, "images", "logo.png");
            bool hasLogo = System.IO.File.Exists(logoPath);
            iText.Layout.Element.Image logo = null!;
            if (hasLogo)
                logo = new iText.Layout.Element.Image(ImageDataFactory.Create(logoPath)).SetHeight(40);

            foreach (var ds in data)
            {
                // HEADER / FOOTER
                var page = pdf.GetLastPage();
                var ps = page.GetPageSize();
                var canvas = new PdfCanvas(page);

                canvas.BeginText()
                    .SetFontAndSize(normal, 10)
                    .MoveText(ps.GetWidth() / 2 - 140, ps.GetTop() - 30)
                    .ShowText("PIDAR – Preclinical Imaging Data Repository")
                    .EndText();

                canvas.BeginText()
                    .SetFontAndSize(normal, 9)
                    .MoveText(ps.GetWidth() / 2 - 15, ps.GetBottom() + 20)
                    .ShowText($"Page {pdf.GetPageNumber(page)}")
                    .EndText();

                canvas.Release();

                // LOGO
                if (hasLogo)
                    doc.Add(new Paragraph().Add(logo));

                // SUMMARY
                var summary = new Paragraph()
                    .Add($"Dataset ID: {ds.DisplayId}\n")
                    .Add($"Species: {GetValue(ds.InVivo, "Species")}\n")
                    .Add($"Organ/Tissue: {GetValue(ds.InVivo, "OrganOrTissue")}\n")
                    .Add($"Disease Model: {GetValue(ds.InVivo, "DiseaseModel")}\n")
                    .Add($"Imaging Modality: {GetValue(ds.StudyComponent, "ImagingModality")}\n")
                    .Add($"Sample Size: {GetValue(ds.InVivo, "OverallSampleSize")}\n")
                    .SetFont(bold).SetFontSize(12)
                    .SetMarginBottom(10);

                doc.Add(summary);

                // SECTIONS
                foreach (var sec in Sections)
                {
                    var obj = sec.Value(ds);
                    if (obj == null) continue;

                    doc.Add(new Paragraph(sec.Key)
                        .SetFont(bold)
                        .SetFontSize(11)
                        .SetBackgroundColor(ColorConstants.LIGHT_GRAY)
                        .SetMarginTop(10));

                    var table = new iText.Layout.Element.Table(UnitValue.CreatePercentArray(new float[] { 30, 70 }))
                        .UseAllAvailableWidth();

                    foreach (var p in obj.GetType().GetProperties())
                    {
                        if (SkipProp(p.Name)) continue;
                        if (!IsSimpleType(p.PropertyType)) continue;

                        var val = p.GetValue(obj);
                        if (val == null) continue;

                        if (val is string s && string.IsNullOrWhiteSpace(s))
                            continue;

                        table.AddCell(new Cell().Add(
                            new Paragraph(Pretty(p.Name)).SetFont(bold).SetFontSize(9)
                        ));

                        table.AddCell(new Cell().Add(
                            new Paragraph(val.ToString()).SetFont(normal).SetFontSize(8)
                        ));
                    }

                    doc.Add(table);
                }

                // Next page
                doc.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
            }

            doc.Close();

            return File(stream.ToArray(), "application/pdf", "PIDAR_datasets.pdf");
        }
    }
}
