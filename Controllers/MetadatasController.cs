using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;
using System.Data;

namespace Pidar.Controllers
{
    [Route("Metadatas")]
    public class MetadatasController : Controller
    {
        private readonly PidarDbContext _context;
        private readonly ILogger<MetadatasController> _logger;



        ////[Route("Download")]
        ////public async Task<IActionResult> Download(string format)
        ////{
        ////    try
        ////    {
        ////        var metadataList = await _context.Metadata.ToListAsync();

        ////        return format.ToLower() switch
        ////        {
        ////            "csv" => DownloadCsv(metadataList),
        ////            "excel" => DownloadExcel(metadataList),
        ////            //"pdf" => DownloadPdf(metadataList),
        ////            //"json" => DownloadJson(metadataList),
        ////            _ => throw new ArgumentException("Invalid format requested")
        ////        };
        ////    }
        ////    catch (OperationCanceledException)
        ////    {
        ////        _logger.LogWarning("Download operation was canceled by the user.");
        ////        return File(Array.Empty<byte>(), "application/octet-stream", "error.txt");
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        _logger.LogError(ex, "Error during file download: {Format}", format);
        ////        return File(Array.Empty<byte>(), "application/octet-stream", "error.txt");
        ////    }
        ////}



        ////[Route("Download")]
        ////public async Task<FileResult> Download(string format)
        ////{
        ////    try
        ////    {
        ////        var metadataList = await _context.Metadata.ToListAsync();

        ////        switch (format.ToLower())
        ////        {
        ////            case "csv":
        ////                return DownloadCsv(metadataList);
        ////            case "excel":
        ////                return DownloadExcel(metadataList);
        ////            case "pdf":
        ////                return DownloadPdf(metadataList);
        ////            case "json":
        ////                return DownloadJson(metadataList);

        ////            default:
        ////                throw new ArgumentException("Invalid format requested");
        ////        }
        ////    }
        ////    catch (Exception ex)
        ////    {
        ////        _logger.LogError(ex, "Error during file download: {Format}", format);
        ////        throw; // Re-throw the exception to return a 500 error
        ////    }
        ////}

        ////private IActionResult DownloadCsv(List<Metadata> metadataList)
        ////{
        ////    using (var memoryStream = new MemoryStream())
        ////    using (var streamWriter = new StreamWriter(memoryStream, Encoding.UTF8))
        ////    using (var csvWriter = new CsvWriter(streamWriter, System.Globalization.CultureInfo.InvariantCulture))
        ////    {
        ////        csvWriter.WriteRecords(metadataList);
        ////        streamWriter.Flush();
        ////        memoryStream.Position = 0;

        ////        return File(memoryStream.ToArray(), "text/csv", "Pidar_Metadata.csv");
        ////    }
        ////}

        ////private FileResult DownloadCsv(List<Metadata> metadataList)
        ////{
        ////    var stream = new MemoryStream();
        ////    using (var writer = new StreamWriter(stream))
        ////    using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
        ////    {
        ////        csv.WriteRecords(metadataList);
        ////    }

        ////    stream.Position = 0;
        ////    return File(stream, "text/csv", "metadata.csv");
        ////}

        ////private IActionResult DownloadExcel(List<Metadata> metadataList)
        ////{
        ////    using (var workbook = new XLWorkbook())
        ////    {
        ////        var worksheet = workbook.Worksheets.Add("Metadata");
        ////        worksheet.Cell(1, 1).InsertTable(metadataList);

        ////        using (var memoryStream = new MemoryStream())
        ////        {
        ////            workbook.SaveAs(memoryStream);
        ////            memoryStream.Position = 0;

        ////            return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Pidar_Metadata.xlsx");
        ////        }
        ////    }
        ////}


        ////private IActionResult DownloadPdf(List<Metadata> metadataList)
        ////{
        ////    using (var memoryStream = new MemoryStream())
        ////    {
        ////        // Initialize PDF writer and document
        ////        var writer = new PdfWriter(memoryStream);
        ////        var pdf = new PdfDocument(writer);
        ////        var document = new Document(pdf, PageSize.A4.Rotate());

        ////        // Define fonts
        ////        PdfFont boldFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA_BOLD);
        ////        PdfFont normalFont = PdfFontFactory.CreateFont(StandardFonts.HELVETICA);

        ////        // Iterate through each metadata item
        ////        foreach (var item in metadataList)
        ////        {
        ////            // Create a table for each item (card-style layout)
        ////            var table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth(); // 2 columns: Key and Value

        ////            // Add rows for non-null and non-empty fields
        ////            foreach (var prop in typeof(Metadata).GetProperties())
        ////            {
        ////                var value = prop.GetValue(item)?.ToString();
        ////                if (!string.IsNullOrEmpty(value)) // Exclude null or empty fields
        ////                {
        ////                    table.AddCell(new Cell().Add(new Paragraph(prop.Name).SetFont(boldFont))); // Key
        ////                    table.AddCell(new Cell().Add(new Paragraph(value).SetFont(normalFont))); // Value
        ////                }
        ////            }

        ////            // Add the table to the document
        ////            document.Add(table);

        ////            // Add a page break if there are more items
        ////            if (metadataList.IndexOf(item) < metadataList.Count - 1)
        ////            {
        ////                document.Add(new AreaBreak(AreaBreakType.NEXT_PAGE));
        ////            }
        ////        }

        ////        document.Close();
        ////        return File(memoryStream.ToArray(), "application/pdf", "Pidar_Metadata.pdf");
        ////    }
        ////}
        ///*-------------------------------------------Download functionality-------------------------------------------*/

        ////private FileResult DownloadCsv(List<Metadata> metadataList)
        ////{
        ////    using var memoryStream = new MemoryStream();
        ////    using var streamWriter = new StreamWriter(memoryStream);
        ////    using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);

        ////    // Write headers dynamically
        ////    csvWriter.WriteHeader(metadataList.First().GetType());
        ////    csvWriter.NextRecord();

        ////    // Write records
        ////    csvWriter.WriteRecords(metadataList);
        ////    streamWriter.Flush();

        ////    return File(memoryStream.ToArray(), "text/csv", "metadata.csv");
        ////}

        ////private FileResult DownloadExcel(List<Metadata> metadataList)
        ////{
        ////    using var workbook = new XLWorkbook();
        ////    var worksheet = workbook.Worksheets.Add("Metadata");

        ////    // Write headers dynamically
        ////    var properties = metadataList.First().GetType().GetProperties();
        ////    for (int i = 0; i < properties.Length; i++)
        ////    {
        ////        worksheet.Cell(1, i + 1).Value = properties[i].Name;
        ////    }

        ////    // Write records
        ////    for (int i = 0; i < metadataList.Count; i++)
        ////    {
        ////        for (int j = 0; j < properties.Length; j++)
        ////        {
        ////            worksheet.Cell(i + 2, j + 1).Value = properties[j].GetValue(metadataList[i])?.ToString();
        ////        }
        ////    }

        ////    using var memoryStream = new MemoryStream();
        ////    workbook.SaveAs(memoryStream);
        ////    return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "metadata.xlsx");
        ////}
        ////private FileResult DownloadPdf(List<Metadata> metadataList)
        ////{
        ////    using var memoryStream = new MemoryStream();
        ////    var writer = new PdfWriter(memoryStream);
        ////    var pdf = new PdfDocument(writer);
        ////    var document = new Document(pdf, PageSize.A4.Rotate()); // Landscape mode

        ////    // Get properties (columns) of the Metadata class
        ////    var properties = metadataList.First().GetType().GetProperties();

        ////    // Create a table with 2 columns (key-value pairs)
        ////    var table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth(); // Fit table to page width
        ////    table.SetWidth(UnitValue.CreatePercentValue(100)); // 100% width

        ////    // Add headers
        ////    table.AddHeaderCell("Key");
        ////    table.AddHeaderCell("Value");

        ////    // Add rows
        ////    foreach (var item in metadataList)
        ////    {
        ////        foreach (var prop in properties)
        ////        {
        ////            // Add key (property name)
        ////            table.AddCell(new Paragraph(prop.Name).SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)));

        ////            // Add value (property value)
        ////            var value = prop.GetValue(item)?.ToString() ?? "N/A";
        ////            table.AddCell(new Paragraph(value).SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)));
        ////        }

        ////        // Add a separator between records
        ////        table.AddCell(new Paragraph("").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)));
        ////        table.AddCell(new Paragraph("").SetFont(PdfFontFactory.CreateFont(StandardFonts.HELVETICA)));
        ////    }

        ////    // Add table to document
        ////    document.Add(table);

        ////    // Close the document
        ////    document.Close();

        ////    return File(memoryStream.ToArray(), "application/pdf", "metadata.pdf");
        ////}

        ////private FileResult DownloadJson(List<Metadata> metadataList)
        ////{
        ////    var json = JsonSerializer.Serialize(metadataList);
        ////    var bytes = Encoding.UTF8.GetBytes(json);
        ////    return File(bytes, "application/json", "metadata.json");
        ////}

        ////private FileResult DownloadCsv(IEnumerable<Metadata> metadataList)
        ////{
        ////    var csv = new StringBuilder();
        ////    var properties = typeof(Metadata).GetProperties();

        ////    // Write headers
        ////    csv.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        ////    // Write data rows
        ////    foreach (var item in metadataList)
        ////    {
        ////        csv.AppendLine(string.Join(",", properties.Select(p => p.GetValue(item)?.ToString())));
        ////    }

        ////    var bytes = Encoding.UTF8.GetBytes(csv.ToString());
        ////    return File(bytes, "text/csv", "metadata.csv");
        ////}
        ////private FileResult DownloadExcel(IEnumerable<Metadata> metadataList)
        ////{
        ////    using var workbook = new XLWorkbook();
        ////    var worksheet = workbook.Worksheets.Add("Metadata");

        ////    var properties = typeof(Metadata).GetProperties();

        ////    // Add headers (first row)
        ////    for (int col = 0; col < properties.Length; col++)
        ////    {
        ////        worksheet.Cell(1, col + 1).Value = properties[col].Name;
        ////        worksheet.Cell(1, col + 1).Style.Font.Bold = true; // Make headers bold
        ////    }

        ////    // Add rows (data)
        ////    int row = 2;
        ////    foreach (var item in metadataList)
        ////    {
        ////        for (int col = 0; col < properties.Length; col++)
        ////        {
        ////            worksheet.Cell(row, col + 1).Value = properties[col].GetValue(item)?.ToString() ?? "";
        ////        }
        ////        row++;
        ////    }

        ////    // Auto-fit columns for better readability
        ////    worksheet.Columns().AdjustToContents();

        ////    // Save to MemoryStream
        ////    using var stream = new MemoryStream();
        ////    workbook.SaveAs(stream);
        ////    stream.Position = 0; // Reset position before returning

        ////    return File(stream.ToArray(),
        ////        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ////        "metadata.xlsx");
        ////}


        ////private FileResult DownloadPdf(IEnumerable<Metadata> metadataList)
        ////{
        ////    using var stream = new MemoryStream();

        ////    // Specify the correct Document class from iTextSharp
        ////    iTextSharp.text.Document document = new iTextSharp.text.Document();

        ////    // Specify the correct PdfWriter from iTextSharp
        ////    iTextSharp.text.pdf.PdfWriter.GetInstance(document, stream);

        ////    document.Open();

        ////    var table = new iTextSharp.text.pdf.PdfPTable(typeof(Metadata).GetProperties().Length);

        ////    var properties = typeof(Metadata).GetProperties();

        ////    // Add headers
        ////    foreach (var prop in properties)
        ////    {
        ////        table.AddCell(new iTextSharp.text.Phrase(prop.Name));
        ////    }

        ////    // Add data rows
        ////    foreach (var item in metadataList)
        ////    {
        ////        foreach (var prop in properties)
        ////        {
        ////            table.AddCell(new iTextSharp.text.Phrase(prop.GetValue(item)?.ToString()));
        ////        }
        ////    }

        ////    document.Add(table);
        ////    document.Close();

        ////    return File(stream.ToArray(), "application/pdf", "metadata.pdf");
        ////}

        ////private FileResult DownloadJson(IEnumerable<Metadata> metadataList)
        ////{
        ////    var json = JsonSerializer.Serialize(metadataList, new JsonSerializerOptions { WriteIndented = true });
        ////    var bytes = Encoding.UTF8.GetBytes(json);
        ////    return File(bytes, "application/json", "metadata.json");
        ////}

        //[HttpGet]
        //[Route("Download")]
        //public async Task<IActionResult> Download(string format)
        //{
        //    var metadataList = await _context.Metadata.ToListAsync();

        //    switch (format.ToLower())
        //    {
        //        case "csv":
        //            return DownloadCsv(metadataList);
        //        case "excel":
        //            return DownloadExcel(metadataList);
        //        default:
        //            return NotFound();
        //    }
        //}

        //private IActionResult DownloadCsv(List<Metadata> metadataList)
        //{
        //    var csvBuilder = new StringBuilder();

        //    // Dynamically add headers
        //    var properties = typeof(Metadata).GetProperties();
        //    csvBuilder.AppendLine(string.Join(",", properties.Select(p => p.Name)));

        //    // Dynamically add data rows
        //    foreach (var item in metadataList)
        //    {
        //        var rowValues = properties.Select(p => EscapeCsvValue(p.GetValue(item)?.ToString() ?? string.Empty));
        //        csvBuilder.AppendLine(string.Join(",", rowValues));
        //    }

        //    return File(Encoding.UTF8.GetBytes(csvBuilder.ToString()), "text/csv", "Pidar_Metadata.csv");
        //}

        //// Helper method to escape CSV values (e.g., handle commas and quotes)
        //private string EscapeCsvValue(string value)
        //{
        //    if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
        //    {
        //        value = $"\"{value.Replace("\"", "\"\"")}\"";
        //    }
        //    return value;
        //}

        //private IActionResult DownloadExcel(List<Metadata> metadataList)
        //{
        //    using (var workbook = new XLWorkbook())
        //    {
        //        var worksheet = workbook.Worksheets.Add("Metadata");

        //        // Dynamically add headers
        //        var properties = typeof(Metadata).GetProperties();
        //        for (int i = 0; i < properties.Length; i++)
        //        {
        //            worksheet.Cell(1, i + 1).Value = properties[i].Name;
        //        }

        //        // Dynamically add data rows
        //        for (int row = 0; row < metadataList.Count; row++)
        //        {
        //            for (int col = 0; col < properties.Length; col++)
        //            {
        //                var value = properties[col].GetValue(metadataList[row])?.ToString() ?? string.Empty;
        //                worksheet.Cell(row + 2, col + 1).Value = value;
        //            }
        //        }

        //        using (var memoryStream = new MemoryStream())
        //        {
        //            workbook.SaveAs(memoryStream);
        //            memoryStream.Position = 0;
        //            return File(memoryStream.ToArray(), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Pidar_Metadata.xlsx");
        //        }
        //    }
        //}


        /*-------------------------------------------API end points-------------------------------------------*/
        public MetadatasController(PidarDbContext context, ILogger<MetadatasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // Existing actions...

        /// <summary>
        /// Fetches all metadata records.
        /// </summary>
        /// <returns>A list of metadata records in JSON format.</returns>
        [HttpGet("api/metadata")]
        public async Task<IActionResult> GetMetadata()
        {
            try
            {
                var metadataList = await _context.Metadata.ToListAsync();
                return Ok(metadataList);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching metadata");
                return StatusCode(500, "An error occurred while fetching metadata.");
            }
        }

        /// <summary>
        /// Fetches a specific metadata record by its DatasetId.
        /// </summary>
        /// <param name="id">The DatasetId of the metadata record.</param>
        /// <returns>The metadata record in JSON format.</returns>
        [HttpGet("api/metadata/{id}")]
        public async Task<IActionResult> GetMetadataById(int id)
        {
            try
            {
                var metadata = await _context.Metadata.FindAsync(id);
                if (metadata == null)
                {
                    return NotFound();
                }
                return Ok(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching metadata with ID {Id}", id);
                return StatusCode(500, "An error occurred while fetching metadata.");
            }
        }


       


        /*-------------------------------------------------------------------------------------------------*/

        // Modify the existing Index method to handle both sorting and pagination
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string sortOrder, int pageNumber = 1)
        {
            // Sorting logic
            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";
            ViewBag.CurrentSort = sortOrder;

            var query = _context.Metadata.AsQueryable();

            // Determine the sorting order
            query = sortOrder switch
            {
                "displayid_asc" => query.OrderBy(m => m.DisplayId),
                "displayid_desc" => query.OrderByDescending(m => m.DisplayId),
                _ => query.OrderBy(m => m.DisplayId) // Default sorting
            };

            // Pagination logic
            int pageSize = 10;
            var paginatedData = await PaginatedList<Metadata>.CreateAsync(query, pageNumber, pageSize);

            return View(paginatedData);
        }



        

      

        // GET: Metadatas/ShowSearchForm
        [Route("Search")]
        public async Task<IActionResult> ShowSearchForm(int pageNumber = 1)
        {
            int pageSize = 10; // Show 10 items per page
            var query = _context.Metadata.AsQueryable(); // Adjust according to your DbSet
            var paginatedData = await PaginatedList<Metadata>.CreateAsync(query, pageNumber, pageSize);

            return View(paginatedData);
        }

        // POST: Metadatas/ShowSearchResults
        [Route("SearchResults")]
        public async Task<IActionResult> ShowSearchResults(string SearchPhrase, int pageNumber = 1)
        {
            int pageSize = 10;

            var query = _context.Metadata.AsQueryable(); // Stay in IQueryable

            if (!string.IsNullOrWhiteSpace(SearchPhrase))
            {
                var allData = await query.ToListAsync(); // Load data into memory

                var results = allData.Where(m =>
                    typeof(Metadata).GetProperties()
                        .Where(p => p.PropertyType == typeof(string)) // Only string properties
                        .Select(p => p.GetValue(m) as string) // Get values
                        .Any(value => value != null && value.Contains(SearchPhrase, StringComparison.OrdinalIgnoreCase)) // Case-insensitive search
                ).ToList();

                int totalRecords = results.Count;

                var paginatedResults = results
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                return View("Index", new PaginatedList<Metadata>(paginatedResults, totalRecords, pageNumber, pageSize));
            }

            int totalRecordsDb = await query.CountAsync();
            var paginatedData = await query
                .OrderBy(m => m.DatasetId)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return View("Index", new PaginatedList<Metadata>(paginatedData, totalRecordsDb, pageNumber, pageSize));
        }


        



        // GET: Metadatas/Details/5
        [Route("Details")]
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var metadata = await _context.Metadata
                .FirstOrDefaultAsync(m => m.DisplayId == id);
            if (metadata == null)
            {
                return NotFound();
            }

            return View(metadata);
        }

        // GET: Metadatas/Create
        [Authorize]
        [Route("Create")]
        public IActionResult Create()
        {
            // Get the maximum current DisplayId + 1
            int? maxDisplayId = _context.Metadata.Max(m => (int?)m.DisplayId);
            var suggestedDisplayId = (maxDisplayId ?? 0) + 1;

            ViewBag.SuggestedDisplayId = suggestedDisplayId;
            return View();
        }

        // POST: Metadatas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("Create")]
        
        public async Task<IActionResult> Create([Bind("DisplayId,StudyDesignBackground,StudyDescription,StudyType,StudySubtype,PaperLinked,PaperTitle,PaperAuthors,Affiliation,PaperJournal,PaperYear,PaperDoi,OpenAccess,UpdatedPaperYear,MultiModalityImages,ImagingModality,ImagingSubModality,Radiation,ImagingCoverage,ImagingTarget,Institution,Pi,CoPi,CountryOfInstitution,ImagingFacility,EuroBioImagingNode,CountryOfImagingFacility,LinkToDataset,Funding,FundingAgency,GrantNumber,DatasetAccess,License,LicenseFile,DuoDataUsePermission,DuoDataUseModifier,DuoInvestigation,ContactPerson,NumberOfGroups,TypesOfGroups,OverallSampleSize,DiseaseModel,OrganOrTissue,SampleSizeForEachGroup,PowerCalculation,InclusionCriteria,ExclusionCriteria,Randomization,Blinding,ProceduresToKeepTreatmentsBlind,ProceduresToKeepExperimenterBlind,OutcomeMeasures,StatisticalMethods,Species,Strain,ImmuneStatus,Sex,Age,AgeAtStartExperiment,AgeAtScanningExperimentS,Weight,WeightAtStartExperiment,WeightAtEndExperiment,Genotype,GeneticManipulation,Gene,SourceOfAnimals,RegistryNumberOfAnimalAuthorization,PharmacologicalProceduresInterventionAndControl,PharmacologicalDrug,Company,Formulation,DrugDose,Volume,Concentration,SiteRouteOfAdministration,FrequencyOfAdministration,VehicleOrCarrierSolutionFormulation,DrugBatchSampleNumber,BloodSampling,BloodSamplingMethod,BloodSampleVolume,BloodTiming,SurgicalProceduresIncludingShamSurgery,DescriptionOfTheSurgicalProcedure,ReferenceToProtocol,TargetOrganTissue,PathogenInfectionInterventionAndControl,InfectiousType,InfectiousAgent,DoseLoad,SiteAndRouteOfInfection,TimingOrFrequencyOfInfection,AnalgesicPlanToRelievePainSufferingAndDistress,AnalgesicName,Route,AnalgesicDose,AnesthesiaForImaging,AnesthesiaType,Duration,AnesthesiaDrugs,AnesthesiaDose,MonitoringRegime,Euthanasia,Method,Histology,TissuesCollectedPostEuthanasia,TimingOfCollection,HistologicalProcedure,NameOfReagentS,CatalogueNumber,LengthOfFixation,Imaging,FrequencyOfImaging,TimingOfImaging,OverallScanLength,ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule,ContrastAgentCommercialDrug,ContrastAgentChemicalDrug,ContrastAgentDose,InjectionVolume,InjectionTime,Vehicle,RouteOfAdministration,CellLines,CellLine,Provenance,ModifiedCellLine,TypeOfGeneticModification,GeneModified,VerificationAndAuthentication,CellInjectionRoute,NumberOfCells,Reagents,NameOfReagent,CatalogueNumbers,EquipmentAndSoftware,Manufacturer,ModelVersionNumber,FrequencyOfExperimentalProcedures,TimingOfExperimentalProcedures,FrequencyOfExperimentalMeasurements,TimingOfExperimentalMeasurements,HousingRoom,DietaryIntervention,RespirationRate,BodyTempuratureEtc,FoodIntakeMeasured,InstrumentVendor,InstrumentType,InstrumentSpecifics,ImageAcquisitionParameters,Correction,RawData,QaQc,ImageType,ImageScale,FormatCompression,Dimensions,OverallNumberOfImages,FieldOfView,DimensionExtents,SizeDescription,PixelVoxelSizeDescription,ImageProcessingMethods,ImageReconstructionAlgorithm,QualityControl,ImageSmoothingOrFilteringAlgorithm,ImageRegistrationAlgorithm,AiEnhancedAlgorithm,QcInfo,Corrections,SpatialAndTemporalAlignment,FiducialsUsed,CoregisteredImages,TransformationMatrixOtherInfo,RelatedImagesAndRelationship,AnalysisResultType,DataUsedForAnalysis,AnalysisMethodAndDetails,FileFormatOfResultFileCsvJsonTxtXlsx,Status,NcitImaging,NcitImagingSubmodality,Doid,NcitAnatomy,NcitSpecies,NcitStrain,ChebiPharmaco,ChebiAnesthesia,ChebiContrastAgentCommercialName,ChebiContrastAgentChemicalName,Clo,NcitGene,UpdatedYear,LinkToDataset1")] Metadata metadata)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogError("ModelState is invalid: " + string.Join("; ", ModelState.Values
                                        .SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage)));

                return View(metadata);
            }

            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    // Do NOT manually set DatasetId or DisplayId
                    _context.Add(metadata);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    // Log full error details
                    _logger.LogError(ex, "Error creating metadata: " + ex.Message);

                    // Extract inner exception details
                    var innerMessage = ex.InnerException?.Message ?? "No inner exception details available.";

                    // Show error message in the UI
                    ModelState.AddModelError("", $"An error occurred while saving: {innerMessage}");

                    return View(metadata);
                }
            }
            return View(metadata);
        }


        // GET: Metadatas/Edit/5
        [Authorize]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                _logger.LogWarning("Edit action called with null ID.");
                return NotFound();
            }

            var metadata = await _context.Metadata.FindAsync(id);
            if (metadata == null)
            {
                _logger.LogWarning($"Metadata with ID {id} not found.");
                return NotFound();
            }
            return View(metadata);
        }

        // POST: Metadatas/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Edit/{id}")]
        // Edit need to bind DatasetID too to match all the columns 
        public async Task<IActionResult> Edit(int id, [Bind("DatasetId,DisplayId,StudyDesignBackground,StudyDescription,StudyType,StudySubtype,PaperLinked,PaperTitle,PaperAuthors,Affiliation,PaperJournal,PaperYear,PaperDoi,OpenAccess,UpdatedPaperYear,MultiModalityImages,ImagingModality,ImagingSubModality,Radiation,ImagingCoverage,ImagingTarget,Institution,Pi,CoPi,CountryOfInstitution,ImagingFacility,EuroBioImagingNode,CountryOfImagingFacility,LinkToDataset,Funding,FundingAgency,GrantNumber,DatasetAccess,License,LicenseFile,DuoDataUsePermission,DuoDataUseModifier,DuoInvestigation,ContactPerson,NumberOfGroups,TypesOfGroups,OverallSampleSize,DiseaseModel,OrganOrTissue,SampleSizeForEachGroup,PowerCalculation,InclusionCriteria,ExclusionCriteria,Randomization,Blinding,ProceduresToKeepTreatmentsBlind,ProceduresToKeepExperimenterBlind,OutcomeMeasures,StatisticalMethods,Species,Strain,ImmuneStatus,Sex,Age,AgeAtStartExperiment,AgeAtScanningExperimentS,Weight,WeightAtStartExperiment,WeightAtEndExperiment,Genotype,GeneticManipulation,Gene,SourceOfAnimals,RegistryNumberOfAnimalAuthorization,PharmacologicalProceduresInterventionAndControl,PharmacologicalDrug,Company,Formulation,DrugDose,Volume,Concentration,SiteRouteOfAdministration,FrequencyOfAdministration,VehicleOrCarrierSolutionFormulation,DrugBatchSampleNumber,BloodSampling,BloodSamplingMethod,BloodSampleVolume,BloodTiming,SurgicalProceduresIncludingShamSurgery,DescriptionOfTheSurgicalProcedure,ReferenceToProtocol,TargetOrganTissue,PathogenInfectionInterventionAndControl,InfectiousType,InfectiousAgent,DoseLoad,SiteAndRouteOfInfection,TimingOrFrequencyOfInfection,AnalgesicPlanToRelievePainSufferingAndDistress,AnalgesicName,Route,AnalgesicDose,AnesthesiaForImaging,AnesthesiaType,Duration,AnesthesiaDrugs,AnesthesiaDose,MonitoringRegime,Euthanasia,Method,Histology,TissuesCollectedPostEuthanasia,TimingOfCollection,HistologicalProcedure,NameOfReagentS,CatalogueNumber,LengthOfFixation,Imaging,FrequencyOfImaging,TimingOfImaging,OverallScanLength,ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule,ContrastAgentCommercialDrug,ContrastAgentChemicalDrug,ContrastAgentDose,InjectionVolume,InjectionTime,Vehicle,RouteOfAdministration,CellLines,CellLine,Provenance,ModifiedCellLine,TypeOfGeneticModification,GeneModified,VerificationAndAuthentication,CellInjectionRoute,NumberOfCells,Reagents,NameOfReagent,CatalogueNumbers,EquipmentAndSoftware,Manufacturer,ModelVersionNumber,FrequencyOfExperimentalProcedures,TimingOfExperimentalProcedures,FrequencyOfExperimentalMeasurements,TimingOfExperimentalMeasurements,HousingRoom,DietaryIntervention,RespirationRate,BodyTempuratureEtc,FoodIntakeMeasured,InstrumentVendor,InstrumentType,InstrumentSpecifics,ImageAcquisitionParameters,Correction,RawData,QaQc,ImageType,ImageScale,FormatCompression,Dimensions,OverallNumberOfImages,FieldOfView,DimensionExtents,SizeDescription,PixelVoxelSizeDescription,ImageProcessingMethods,ImageReconstructionAlgorithm,QualityControl,ImageSmoothingOrFilteringAlgorithm,ImageRegistrationAlgorithm,AiEnhancedAlgorithm,QcInfo,Corrections,SpatialAndTemporalAlignment,FiducialsUsed,CoregisteredImages,TransformationMatrixOtherInfo,RelatedImagesAndRelationship,AnalysisResultType,DataUsedForAnalysis,AnalysisMethodAndDetails,FileFormatOfResultFileCsvJsonTxtXlsx,Status,NcitImaging,NcitImagingSubmodality,Doid,NcitAnatomy,NcitSpecies,NcitStrain,ChebiPharmaco,ChebiAnesthesia,ChebiContrastAgentCommercialName,ChebiContrastAgentChemicalName,Clo,NcitGene,UpdatedYear,LinkToDataset1")] Metadata metadata)
        {

            if (id != metadata.DatasetId)  
            {
                return NotFound();
            }
           

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(metadata);
                    await _context.SaveChangesAsync();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!MetadataExists(metadata.DatasetId))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }               

            }
            
            return View(metadata);
        }
        private bool MetadataExists(int id)
        {
            return _context.Metadata.Any(e => e.DatasetId== id);
        }



        // GET: Metadatas/Delete/5
        [Authorize]
        [Route("Delete/{id}")]
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var metadata = await _context.Metadata
                .FirstOrDefaultAsync(m => m.DatasetId == id);
            if (metadata == null)
            {
                return NotFound();
            }

            return View(metadata);
        }

        // POST: Metadatas/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        [Route("Delete/{id}")]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Find the record to delete
                var metadata = await _context.Metadata.FindAsync(id);
                if (metadata == null) return NotFound();

                // 2. Store the DisplayId of the deleted record
                int deletedDisplayId = metadata.DisplayId;
                _logger.LogInformation($"Deleting record with DisplayId: {deletedDisplayId}");

                // 3. Delete the record
                _context.Metadata.Remove(metadata);
                await _context.SaveChangesAsync();

                // 4. Decrement DisplayId for all records with DisplayId > deletedDisplayId
                var recordsToUpdate = await _context.Metadata
                    .Where(m => m.DisplayId > deletedDisplayId)
                    .OrderBy(m => m.DisplayId)
                    .ToListAsync();

                _logger.LogInformation($"Found {recordsToUpdate.Count} records to update.");

                foreach (var record in recordsToUpdate)
                {
                    record.DisplayId -= 1;
                    _context.Update(record);
                    _logger.LogInformation($"Updated record with DatasetId: {record.DatasetId} to DisplayId: {record.DisplayId}");
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error deleting metadata");
                ModelState.AddModelError("", "An error occurred while deleting.");
                return View("Error"); // Or handle the error appropriately
            }
        }

        
    }




}


