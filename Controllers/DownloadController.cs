using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.IO;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using ClosedXML.Excel;
using System.Text.Json;
using System.Text;
using Pidar.Data;
using Pidar.Models;
using iText.Layout.Properties;
using iText.Kernel.Geom;
using iText.IO.Font.Constants;
using iText.Kernel.Font;


namespace Pidar.Controllers
{
    public class DownloadController : Controller
    {
        private readonly PidarDbContext _context;

        public DownloadController(PidarDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
        }

        // CSV Download
        public IActionResult DownloadCsv()
        {
            var metadataList = _context.Metadata.ToList();
            var csvContent = GenerateCsv(metadataList);
            return File(new System.Text.UTF8Encoding().GetBytes(csvContent), "text/csv", "metadata.csv");
        }

        // PDF Download using iText7
        public IActionResult DownloadPdf()
        {
            // Fetch data from the database
            var metadataList = _context.Metadata.ToList();

            // Debug: Check if data is being retrieved
            Console.WriteLine($"Total rows: {metadataList.Count}");

            // Create a memory stream to hold the PDF
            var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4.Rotate()); // Landscape mode for better readability

            // Set page margins
            document.SetMargins(20, 20, 20, 20);

            // Get the properties of the Metadata class
            var properties = typeof(Metadata).GetProperties();

            // Create a bold font for headers
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Create a regular font for data
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Iterate through each metadata row
            for (int i = 0; i < metadataList.Count; i++)
            {
                var metadata = metadataList[i];

                // Debug: Check current row data
                Console.WriteLine($"Processing row: {metadata.DatasetId}");

                // Filter properties with non-null values for the current row
                var nonNullProperties = properties
                    .Where(prop => prop.GetValue(metadata) != null)
                    .ToList();

                // Debug: Check filtered properties
                foreach (var property in nonNullProperties)
                {
                    Console.WriteLine($"{property.Name}: {property.GetValue(metadata)}");
                }

                // Create a table with 2 columns (key-value pairs)
                var table = new Table(UnitValue.CreatePercentArray(new float[] { 30, 70 })).UseAllAvailableWidth(); // 30% for keys, 70% for values

                // Add key-value pairs for the current row
                foreach (var property in nonNullProperties)
                {
                    // Get the key (property name)
                    var key = property.Name;

                    // Get the value (property value)
                    var value = property.GetValue(metadata)?.ToString();

                    // Add the key-value pair to the table
                    table.AddCell(new Cell().Add(new Paragraph(key).SetFont(boldFont).SetFontSize(10)));
                    table.AddCell(new Cell().Add(new Paragraph(value).SetFont(regularFont).SetFontSize(8)));
                }

                // Add the table to the document
                document.Add(table);

                // Add a page break after each dataset (except the last one)
                if (i < metadataList.Count - 1)
                {
                    Console.WriteLine($"Adding page break after row {i}");
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            // Close the document
            document.Close();

            // Return the PDF as a file
            return File(stream.ToArray(), "application/pdf", "Pidar_metadata.pdf");
        }

        // JSON Download
        public IActionResult DownloadJson()
        {
            var metadataList = _context.Metadata.ToList();
            var jsonContent = JsonSerializer.Serialize(metadataList);
            return File(new System.Text.UTF8Encoding().GetBytes(jsonContent), "application/json", "Pidar_metadata.json");
        }

        // XLSX Download using ClosedXML
        public IActionResult DownloadXlsx()
        {
            var metadataList = _context.Metadata.ToList();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Metadata");
                // Add headers
                var properties = typeof(Metadata).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i].Name;
                }

                // Add data
                for (int i = 0; i < metadataList.Count; i++)
                {
                    for (int j = 0; j < properties.Length; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value = properties[j].GetValue(metadataList[i])?.ToString();
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Pidar_metadata.xlsx");
                }
            }
        }

        private string GenerateCsv(List<Metadata> metadataList)
        {
            var sb = new StringBuilder();
            var properties = typeof(Metadata).GetProperties();

            // Add headers
            sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            // Add data
            foreach (var metadata in metadataList)
            {
                sb.AppendLine(string.Join(",", properties.Select(p => p.GetValue(metadata)?.ToString())));
            }

            return sb.ToString();
        }
    }
}
