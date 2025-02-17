﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Models;
using System.Data;
using Microsoft.Extensions.Logging;

namespace Pidar.Controllers
{
    [Route("Metadatas")]
    public class MetadatasController : Controller
    {
        private readonly PidarDbContext _context;
        private readonly ILogger<MetadatasController> _logger;

        // Modify the existing Index method to handle both sorting and pagination
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index(string sortOrder, int pageNumber = 1)
        {
            // Sorting logic
            ViewData["DatasetIdSortParam"] = sortOrder == "datasetid_asc" ? "datasetid_desc" : "datasetid_asc";
            ViewBag.CurrentSort = sortOrder;

            var query = _context.Metadata.AsQueryable();

            // Determine the sorting order
            query = sortOrder switch
            {
                "datasetid_asc" => query.OrderBy(m => m.DatasetId),
                "datasetid_desc" => query.OrderByDescending(m => m.DatasetId),
                _ => query.OrderBy(m => m.DatasetId) // Default sorting
            };

            // Pagination logic
            int pageSize = 5;
            var paginatedData = await PaginatedList<Metadata>.CreateAsync(query, pageNumber, pageSize);

            return View(paginatedData);
        }



        public MetadatasController(PidarDbContext context, ILogger<MetadatasController> logger)
        {
            _context = context;
            _logger = logger;
        }

      

        // GET: Metadatas/ShowSearchForm
        [Route("Search")]
        public async Task<IActionResult> ShowSearchForm(int pageNumber = 1)
        {
            int pageSize = 5; // Show 5 items per page
            var query = _context.Metadata.AsQueryable(); // Adjust according to your DbSet
            var paginatedData = await PaginatedList<Metadata>.CreateAsync(query, pageNumber, pageSize);

            return View(paginatedData);
        }

        // POST: Metadatas/ShowSearchResults
        [Route("SearchResults")]
        public async Task<IActionResult> ShowSearchResults(string SearchPhrase, int pageNumber = 1)
        {
            int pageSize = 5;

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
                .FirstOrDefaultAsync(m => m.DatasetId == id);
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
            return View();
        }

        // POST: Metadatas/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to.
        // For more details, see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize]
        [Route("Create")]
        public async Task<IActionResult> Create(Metadata metadata)
        {
            if (ModelState.IsValid)
            {
                using var transaction = await _context.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable);
                try
                {
                    // Get current max ID safely
                    int maxId = await _context.Metadata.MaxAsync(m => (int?)m.DatasetId) ?? 0;
                    metadata.DatasetId = maxId + 1;

                    _context.Add(metadata);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Error creating metadata");
                    ModelState.AddModelError("", "An error occurred while saving.");
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
                return NotFound();
            }

            var metadata = await _context.Metadata.FindAsync(id);
            if (metadata == null)
            {
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
        public async Task<IActionResult> Edit(int id, [Bind("DatasetId,StudyDesignBackground,StudyDescription,StudyType,StudySubtype,PaperLinked,PaperTitle,PaperAuthors,Affiliation,PaperJournal,PaperYear,PaperDoi,OpenAccess,UpdatedPaperYear,MultiModalityImages,ImagingModality,ImagingSubModality,Radiation,ImagingCoverage,ImagingTarget,Institution,Pi,CoPi,CountryOfInstitution,ImagingFacility,EuroBioImagingNode,CountryOfImagingFacility,LinkToDataset,Funding,FundingAgency,GrantNumber,DatasetAccess,License,LicenseFile,DuoDataUsePermission,DuoDataUseModifier,DuoInvestigation,ContactPerson,NumberOfGroups,TypesOfGroups,OverallSampleSize,DiseaseModel,OrganOrTissue,SampleSizeForEachGroup,PowerCalculation,InclusionCriteria,ExclusionCriteria,Randomization,Blinding,ProceduresToKeepTreatmentsBlind,ProceduresToKeepExperimenterBlind,OutcomeMeasures,StatisticalMethods,Species,Strain,ImmuneStatus,Sex,Age,AgeAtStartExperiment,AgeAtScanningExperimentS,Weight,WeightAtStartExperiment,WeightAtEndExperiment,Genotype,GeneticManipulation,Gene,SourceOfAnimals,RegistryNumberOfAnimalAuthorization,PharmacologicalProceduresInterventionAndControl,PharmacologicalDrug,Company,Formulation,DrugDose,Volume,Concentration,SiteRouteOfAdministration,FrequencyOfAdministration,VehicleOrCarrierSolutionFormulation,DrugBatchSampleNumber,BloodSampling,BloodSamplingMethod,BloodSampleVolume,BloodTiming,SurgicalProceduresIncludingShamSurgery,DescriptionOfTheSurgicalProcedure,ReferenceToProtocol,TargetOrganTissue,PathogenInfectionInterventionAndControl,InfectiousType,InfectiousAgent,DoseLoad,SiteAndRouteOfInfection,TimingOrFrequencyOfInfection,AnalgesicPlanToRelievePainSufferingAndDistress,AnalgesicName,Route,AnalgesicDose,AnesthesiaForImaging,AnesthesiaType,Duration,AnesthesiaDrugs,AnesthesiaDose,MonitoringRegime,Euthanasia,Method,Histology,TissuesCollectedPostEuthanasia,TimingOfCollection,HistologicalProcedure,NameOfReagentS,CatalogueNumber,LengthOfFixation,Imaging,FrequencyOfImaging,TimingOfImaging,OverallScanLength,ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule,ContrastAgentCommercialDrug,ContrastAgentChemicalDrug,ContrastAgentDose,InjectionVolume,InjectionTime,Vehicle,RouteOfAdministration,CellLines,CellLine,Provenance,ModifiedCellLine,TypeOfGeneticModification,GeneModified,VerificationAndAuthentication,CellInjectionRoute,NumberOfCells,Reagents,NameOfReagent,CatalogueNumbers,EquipmentAndSoftware,Manufacturer,ModelVersionNumber,FrequencyOfExperimentalProcedures,TimingOfExperimentalProcedures,FrequencyOfExperimentalMeasurements,TimingOfExperimentalMeasurements,HousingRoom,DietaryIntervention,RespirationRate,BodyTempuratureEtc,FoodIntakeMeasured,InstrumentVendor,InstrumentType,InstrumentSpecifics,ImageAcquisitionParameters,Correction,RawData,QaQc,ImageType,ImageScale,FormatCompression,Dimensions,OverallNumberOfImages,FieldOfView,DimensionExtents,SizeDescription,PixelVoxelSizeDescription,ImageProcessingMethods,ImageReconstructionAlgorithm,QualityControl,ImageSmoothingOrFilteringAlgorithm,ImageRegistrationAlgorithm,AiEnhancedAlgorithm,QcInfo,Corrections,SpatialAndTemporalAlignment,FiducialsUsed,CoregisteredImages,TransformationMatrixOtherInfo,RelatedImagesAndRelationship,AnalysisResultType,DataUsedForAnalysis,AnalysisMethodAndDetails,FileFormatOfResultFileCsvJsonTxtXlsx,Status,NcitImaging,NcitImagingSubmodality,Doid,NcitAnatomy,NcitSpecies,NcitStrain,ChebiPharmaco,ChebiAnesthesia,ChebiContrastAgentCommercialName,ChebiContrastAgentChemicalName,Clo,NcitGene,UpdatedYear,LinkToDataset1")] Metadata metadata)
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
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // 1. Delete the target record
                var metadata = await _context.Metadata.FindAsync(id);
                if (metadata == null) return NotFound();

                _context.Metadata.Remove(metadata);
                await _context.SaveChangesAsync();

                // 2. Renumber subsequent records
                var recordsToUpdate = await _context.Metadata
                    .Where(m => m.DatasetId > id)
                    .OrderBy(m => m.DatasetId)
                    .ToListAsync();

                foreach (var record in recordsToUpdate)
                {
                    record.DatasetId -= 1;
                    _context.Update(record);
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}


