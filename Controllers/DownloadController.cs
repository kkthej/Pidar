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
using System.Data;


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
            var datasetList = _context.Dataset.ToList();
            var csvContent = GenerateCsv(datasetList);
            return File(new System.Text.UTF8Encoding().GetBytes(csvContent), "text/csv", "dataset.csv");
        }

        // PDF Download using iText7
        public IActionResult DownloadPdf()
        {
            // Fetch data from the database
            var datasetList = _context.Dataset.ToList();

            // Debug: Check if data is being retrieved
            Console.WriteLine($"Total rows: {datasetList.Count}");

            // Create a memory stream to hold the PDF
            var stream = new MemoryStream();
            var writer = new PdfWriter(stream);
            var pdf = new PdfDocument(writer);
            var document = new Document(pdf, PageSize.A4.Rotate()); // Landscape mode for better readability

            // Set page margins
            document.SetMargins(20, 20, 20, 20);

            // Get the properties of the dataset class
            var properties = typeof(Dataset).GetProperties();

            // Create a bold font for headers
            var boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);

            // Create a regular font for data
            var regularFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

            // Iterate through each dataset row
            for (int i = 0; i < datasetList.Count; i++)
            {
                var dataset = datasetList[i];

                // Debug: Check current row data
                Console.WriteLine($"Processing row: {dataset.DatasetId}");

                // Filter properties with non-null values for the current row
                var nonNullProperties = properties
                    .Where(prop => prop.GetValue(dataset) != null)
                    .ToList();

                // Debug: Check filtered properties
                foreach (var property in nonNullProperties)
                {
                    Console.WriteLine($"{property.Name}: {property.GetValue(dataset)}");
                }

                // Create a table with 2 columns (key-value pairs)
                var table = new Table(UnitValue.CreatePercentArray(new float[] { 30, 70 })).UseAllAvailableWidth(); // 30% for keys, 70% for values

                // Add key-value pairs for the current row
                foreach (var property in nonNullProperties)
                {
                    // Get the key (property name)
                    var key = property.Name;

                    // Get the value (property value)
                    var value = property.GetValue(dataset)?.ToString();

                    // Add the key-value pair to the table
                    table.AddCell(new Cell().Add(new Paragraph(key).SetFont(boldFont).SetFontSize(10)));
                    table.AddCell(new Cell().Add(new Paragraph(value).SetFont(regularFont).SetFontSize(8)));
                }

                // Add the table to the document
                document.Add(table);

                // Add a page break after each dataset (except the last one)
                if (i < datasetList.Count - 1)
                {
                    Console.WriteLine($"Adding page break after row {i}");
                    document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
                }
            }

            // Close the document
            document.Close();

            // Return the PDF as a file
            return File(stream.ToArray(), "application/pdf", "Pidar_dataset.pdf");
        }

        // JSON Download
        public IActionResult DownloadJson()
        {
            var datasetList = _context.Dataset.ToList();
            var jsonContent = JsonSerializer.Serialize(datasetList);
            return File(new System.Text.UTF8Encoding().GetBytes(jsonContent), "application/json", "Pidar_dataset.json");
        }

        // XLSX Download using ClosedXML
        public IActionResult DownloadXlsx()
        {
            var datasetList = _context.Dataset.ToList();
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Dataset");
                // Add headers
                var properties = typeof(Dataset).GetProperties();
                for (int i = 0; i < properties.Length; i++)
                {
                    worksheet.Cell(1, i + 1).Value = properties[i].Name;
                }

                // Add data
                for (int i = 0; i < datasetList.Count; i++)
                {
                    for (int j = 0; j < properties.Length; j++)
                    {
                        worksheet.Cell(i + 2, j + 1).Value = properties[j].GetValue(datasetList[i])?.ToString();
                    }
                }

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    return File(stream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Pidar_dataset.xlsx");
                }
            }
        }

        private string GenerateCsv(List<Dataset> datasetList)
        {
            var sb = new StringBuilder();
            var properties = typeof(Dataset).GetProperties();

            // Add headers
            sb.AppendLine(string.Join(",", properties.Select(p => p.Name)));

            // Add data
            foreach (var dataset in datasetList)
            {
                sb.AppendLine(string.Join(",", properties.Select(p => p.GetValue(dataset)?.ToString())));
            }

            return sb.ToString();
        }
    }
}
