﻿using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Pidar.Data;
using Pidar.Models;
using System.Data;
using System.Diagnostics;



namespace Pidar.Controllers
{
    [Route("Metadatas")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public class MetadatasController : Controller
    {
        private readonly PidarDbContext _context;
        private readonly ILogger<MetadatasController> _logger;

        public MetadatasController(PidarDbContext context, ILogger<MetadatasController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string sortOrder, int pageNumber = 1)
        {
            ViewData["ActivePage"] = "Metadata";
            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";
            ViewBag.CurrentSort = sortOrder;

            var query = _context.Metadata.AsQueryable();

            query = sortOrder switch
            {
                "displayid_asc" => query.OrderBy(m => m.DisplayId),
                "displayid_desc" => query.OrderByDescending(m => m.DisplayId),
                _ => query.OrderBy(m => m.DisplayId)
            };

            int pageSize = 10;
            var paginatedData = await PaginatedList<Metadata>.CreateAsync(query, pageNumber, pageSize);

            ViewData["MetadataCount"] = await _context.Metadata.CountAsync();
            ViewData["TotalSampleSize"] = await CalculateTotalSampleSizeAsync();
            ViewData["TableColumnCount"] = GetTableColumnCount();

            return View(paginatedData);
        }



        // GET: Metadatas/ShowSearchForm
        [Route("Search")]
        public async Task<IActionResult> ShowSearchForm(int pageNumber = 1)
        {
            int pageSize = 10;
            var query = _context.Metadata.AsQueryable();
            var paginatedData = await PaginatedList<Metadata>.CreateAsync(query, pageNumber, pageSize);
            return View(paginatedData);
        }

        // POST: Metadatas/ShowSearchResults
        [Route("SearchResults")]
        public async Task<IActionResult> ShowSearchResults(string SearchPhrase, string sortOrder, int pageNumber = 1)
        {
           
            int pageSize = 10;

            // Maintain sorting parameters
            ViewData["DisplayIdSortParam"] = sortOrder == "displayid_asc" ? "displayid_desc" : "displayid_asc";
            ViewBag.CurrentSort = sortOrder;

            if (!string.IsNullOrWhiteSpace(SearchPhrase))
            {
                var allData = await _context.Metadata.ToListAsync();

                // Perform case-insensitive search across all string properties
                var results = allData.Where(m =>
                    typeof(Metadata).GetProperties()
                        .Where(p => p.PropertyType == typeof(string))
                        .Select(p => p.GetValue(m)?.ToString())
                        .Any(value => value != null &&
                           value.Contains(SearchPhrase, StringComparison.OrdinalIgnoreCase))
                ).ToList();

                // Apply sorting to search results
                results = sortOrder switch
                {
                    "displayid_desc" => results.OrderByDescending(m => m.DisplayId).ToList(),
                    _ => results.OrderBy(m => m.DisplayId).ToList() // Default ascending
                };

                int totalRecords = results.Count;
                var paginatedResults = results
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewData["MetadataCount"] = totalRecords;
                ViewData["TotalSampleSize"] = CalculateSampleSize(results);
                ViewData["TableColumnCount"] = GetTableColumnCount();

                return View("Index", new PaginatedList<Metadata>(paginatedResults, totalRecords, pageNumber, pageSize));
            }

            return RedirectToAction(nameof(Index));
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

        /*public async Task<IActionResult> Create([Bind("DisplayId,StudyDesignBackground,StudyDescription,StudyType,StudySubtype,PaperLinked,PaperTitle,PaperAuthors,Affiliation,PaperJournal,PaperYear,PaperDoi,OpenAccess,UpdatedPaperYear,MultiModalityImages,ImagingModality,ImagingSubModality,Radiation,ImagingCoverage,ImagingTarget,Institution,Pi,CoPi,CountryOfInstitution,ImagingFacility,EuroBioImagingNode,CountryOfImagingFacility,LinkToDataset,Funding,FundingAgency,GrantNumber,DatasetAccess,License,LicenseFile,DuoDataUsePermission,DuoDataUseModifier,DuoInvestigation,ContactPerson,NumberOfGroups,TypesOfGroups,OverallSampleSize,DiseaseModel,OrganOrTissue,SampleSizeForEachGroup,PowerCalculation,InclusionCriteria,ExclusionCriteria,Randomization,Blinding,ProceduresToKeepTreatmentsBlind,ProceduresToKeepExperimenterBlind,OutcomeMeasures,StatisticalMethods,Species,Strain,ImmuneStatus,Sex,Age,AgeAtStartExperiment,AgeAtScanningExperimentS,Weight,WeightAtStartExperiment,WeightAtEndExperiment,Genotype,GeneticManipulation,Gene,SourceOfAnimals,RegistryNumberOfAnimalAuthorization,PharmacologicalProceduresInterventionAndControl,PharmacologicalDrug,Company,Formulation,DrugDose,Volume,Concentration,SiteRouteOfAdministration,FrequencyOfAdministration,VehicleOrCarrierSolutionFormulation,DrugBatchSampleNumber,BloodSampling,BloodSamplingMethod,BloodSampleVolume,BloodTiming,SurgicalProceduresIncludingShamSurgery,DescriptionOfTheSurgicalProcedure,ReferenceToProtocol,TargetOrganTissue,PathogenInfectionInterventionAndControl,InfectiousType,InfectiousAgent,DoseLoad,SiteAndRouteOfInfection,TimingOrFrequencyOfInfection,AnalgesicPlanToRelievePainSufferingAndDistress,AnalgesicName,Route,AnalgesicDose,AnesthesiaForImaging,AnesthesiaType,Duration,AnesthesiaDrugs,AnesthesiaDose,MonitoringRegime,Euthanasia,Method,Histology,TissuesCollectedPostEuthanasia,TimingOfCollection,HistologicalProcedure,NameOfReagentS,CatalogueNumber,LengthOfFixation,Imaging,FrequencyOfImaging,TimingOfImaging,OverallScanLength,ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule,ContrastAgentCommercialDrug,ContrastAgentChemicalDrug,ContrastAgentDose,InjectionVolume,InjectionTime,Vehicle,RouteOfAdministration,CellLines,CellLine,Provenance,ModifiedCellLine,TypeOfGeneticModification,GeneModified,VerificationAndAuthentication,CellInjectionRoute,NumberOfCells,Reagents,NameOfReagent,CatalogueNumbers,EquipmentAndSoftware,Manufacturer,ModelVersionNumber,FrequencyOfExperimentalProcedures,TimingOfExperimentalProcedures,FrequencyOfExperimentalMeasurements,TimingOfExperimentalMeasurements,HousingRoom,DietaryIntervention,RespirationRate,BodyTempuratureEtc,FoodIntakeMeasured,InstrumentVendor,InstrumentType,InstrumentSpecifics,ImageAcquisitionParameters,Correction,RawData,QaQc,ImageType,ImageScale,FormatCompression,Dimensions,OverallNumberOfImages,FieldOfView,DimensionExtents,SizeDescription,PixelVoxelSizeDescription,ImageProcessingMethods,ImageReconstructionAlgorithm,QualityControl,ImageSmoothingOrFilteringAlgorithm,ImageRegistrationAlgorithm,AiEnhancedAlgorithm,QcInfo,Corrections,SpatialAndTemporalAlignment,FiducialsUsed,CoregisteredImages,TransformationMatrixOtherInfo,RelatedImagesAndRelationship,AnalysisResultType,DataUsedForAnalysis,AnalysisMethodAndDetails,FileFormatOfResultFileCsvJsonTxtXlsx,Status,NcitImaging,NcitImagingSubmodality,Doid,NcitAnatomy,NcitSpecies,NcitStrain,ChebiPharmaco,ChebiAnesthesia,ChebiContrastAgentCommercialName,ChebiContrastAgentChemicalName,Clo,NcitGene,UpdatedYear,LinkToDataset1")] Metadata metadata)
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
        */

        
        public async Task<IActionResult> Create([Bind("DisplayId,StudyDesignBackground,StudyDescription,StudyType,StudySubtype,PaperLinked,PaperTitle,PaperAuthors,Affiliation,PaperJournal,PaperYear,PaperDoi,OpenAccess,UpdatedPaperYear,MultiModalityImages,ImagingModality,ImagingSubModality,Radiation,ImagingCoverage,ImagingTarget,Institution,Pi,CoPi,CountryOfInstitution,ImagingFacility,EuroBioImagingNode,CountryOfImagingFacility,LinkToDataset,Funding,FundingAgency,GrantNumber,DatasetAccess,License,LicenseFile,DuoDataUsePermission,DuoDataUseModifier,DuoInvestigation,ContactPerson,NumberOfGroups,TypesOfGroups,OverallSampleSize,DiseaseModel,OrganOrTissue,SampleSizeForEachGroup,PowerCalculation,InclusionCriteria,ExclusionCriteria,Randomization,Blinding,ProceduresToKeepTreatmentsBlind,ProceduresToKeepExperimenterBlind,OutcomeMeasures,StatisticalMethods,Species,Strain,ImmuneStatus,Sex,Age,AgeAtStartExperiment,AgeAtScanningExperimentS,Weight,WeightAtStartExperiment,WeightAtEndExperiment,Genotype,GeneticManipulation,Gene,SourceOfAnimals,RegistryNumberOfAnimalAuthorization,PharmacologicalProceduresInterventionAndControl,PharmacologicalDrug,Company,Formulation,DrugDose,Volume,Concentration,SiteRouteOfAdministration,FrequencyOfAdministration,VehicleOrCarrierSolutionFormulation,DrugBatchSampleNumber,BloodSampling,BloodSamplingMethod,BloodSampleVolume,BloodTiming,SurgicalProceduresIncludingShamSurgery,DescriptionOfTheSurgicalProcedure,ReferenceToProtocol,TargetOrganTissue,PathogenInfectionInterventionAndControl,InfectiousType,InfectiousAgent,DoseLoad,SiteAndRouteOfInfection,TimingOrFrequencyOfInfection,AnalgesicPlanToRelievePainSufferingAndDistress,AnalgesicName,Route,AnalgesicDose,AnesthesiaForImaging,AnesthesiaType,Duration,AnesthesiaDrugs,AnesthesiaDose,MonitoringRegime,Euthanasia,Method,Histology,TissuesCollectedPostEuthanasia,TimingOfCollection,HistologicalProcedure,NameOfReagentS,CatalogueNumber,LengthOfFixation,Imaging,FrequencyOfImaging,TimingOfImaging,OverallScanLength,ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule,ContrastAgentCommercialDrug,ContrastAgentChemicalDrug,ContrastAgentDose,InjectionVolume,InjectionTime,Vehicle,RouteOfAdministration,CellLines,CellLine,Provenance,ModifiedCellLine,TypeOfGeneticModification,GeneModified,VerificationAndAuthentication,CellInjectionRoute,NumberOfCells,Reagents,NameOfReagent,CatalogueNumbers,EquipmentAndSoftware,Manufacturer,ModelVersionNumber,FrequencyOfExperimentalProcedures,TimingOfExperimentalProcedures,FrequencyOfExperimentalMeasurements,TimingOfExperimentalMeasurements,HousingRoom,DietaryIntervention,RespirationRate,BodyTempuratureEtc,FoodIntakeMeasured,InstrumentVendor,InstrumentType,InstrumentSpecifics,ImageAcquisitionParameters,Correction,RawData,QaQc,ImageType,ImageScale,FormatCompression,Dimensions,OverallNumberOfImages,FieldOfView,DimensionExtents,SizeDescription,PixelVoxelSizeDescription,ImageProcessingMethods,ImageReconstructionAlgorithm,QualityControl,ImageSmoothingOrFilteringAlgorithm,ImageRegistrationAlgorithm,AiEnhancedAlgorithm,QcInfo,Corrections,SpatialAndTemporalAlignment,FiducialsUsed,CoregisteredImages,TransformationMatrixOtherInfo,RelatedImagesAndRelationship,AnalysisResultType,DataUsedForAnalysis,AnalysisMethodAndDetails,FileFormatOfResultFileCsvJsonTxtXlsx,Status,NcitImaging,NcitImagingSubmodality,Doid,NcitAnatomy,NcitSpecies,NcitStrain,ChebiPharmaco,ChebiAnesthesia,ChebiContrastAgentCommercialName,ChebiContrastAgentChemicalName,Clo,NcitGene,UpdatedYear,LinkToDataset1")] Metadata metadata)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogError("ModelState is invalid: " + string.Join("; ", ModelState.Values
                    .SelectMany(v => v.Errors)
                    .Select(e => e.ErrorMessage)));
                return View(metadata);
            }

            var strategy = _context.Database.CreateExecutionStrategy();
            try
            {
                await strategy.ExecuteAsync(async () =>
                {
                    await using var tx = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);

                    // Do NOT manually set DatasetId or DisplayId
                    _context.Add(metadata);
                    await _context.SaveChangesAsync();

                    await tx.CommitAsync();
                });

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex) when (ex.GetBaseException() is Npgsql.PostgresException pg)
            {
                _logger.LogError(ex,
                    "DB error on create. SQLSTATE={SqlState}, Constraint={Constraint}, Detail={Detail}, Hint={Hint}",
                    pg.SqlState, pg.ConstraintName, pg.Detail, pg.Hint);
                ModelState.AddModelError("", $"Database error ({pg.SqlState}): {pg.MessageText}");
                return View(metadata);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating metadata.");
                ModelState.AddModelError("", $"An error occurred while saving: {ex.Message}");
                return View(metadata);
            }
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
            return _context.Metadata.Any(e => e.DatasetId == id);
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
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            IActionResult result = null;

            try
            {
                await executionStrategy.ExecuteAsync(async () =>
                {
                    using var transaction = await _context.Database.BeginTransactionAsync();

                    try
                    {
                        var metadata = await _context.Metadata.FindAsync(id);
                        if (metadata == null)
                        {
                            await transaction.RollbackAsync();
                            result = NotFound(); // ✅ Set the result instead of returning
                            return;
                        }

                        int deletedDisplayId = metadata.DisplayId;
                        _logger.LogInformation($"Deleting record with DisplayId: {deletedDisplayId}");

                        _context.Metadata.Remove(metadata);
                        await _context.SaveChangesAsync();

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

                        result = RedirectToAction(nameof(Index)); // ✅ Set the result here too
                    }
                    catch (Exception ex)
                    {
                        await transaction.RollbackAsync();
                        _logger.LogError(ex, "Error during transaction execution");
                        throw;
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error after retry attempts exhausted");
                result = View("Error", new ErrorViewModel
                {
                    RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier,
                    ErrorMessage = "An error occurred while deleting the record. Please try again later."
                });
            }

            return result; // ✅ Final return here
        }


        #region Helper Methods

        private async Task<int> CalculateTotalSampleSizeAsync()
        {
            var sampleSizes = await _context.Metadata
                .Select(x => x.OverallSampleSize)
                .ToListAsync();

            int total = 0;
            foreach (var size in sampleSizes)
            {
                if (int.TryParse(size?.Replace(",", ""), out int parsedSize))
                {
                    total += parsedSize;
                }
            }
            return total;
        }

        private int CalculateSampleSize(List<Metadata> metadataItems)
        {
            int total = 0;
            foreach (var item in metadataItems)
            {
                if (int.TryParse(item.OverallSampleSize?.Replace(",", ""), out int parsedSize))
                {
                    total += parsedSize;
                }
            }
            return total;
        }

        private int GetTableColumnCount()
        {
            var entityType = _context.Model.FindEntityType(typeof(Metadata));
            return entityType?.GetProperties().Count() ?? 0;
        }

        

        #endregion






    }




}


