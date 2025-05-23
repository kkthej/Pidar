﻿using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Pidar.data.pidar
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "public");

            migrationBuilder.CreateTable(
                name: "metadata",
                schema: "public",
                columns: table => new
                {
                    dataset_id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    display_id = table.Column<int>(type: "integer", nullable: false),
                    StudyDesignBackground = table.Column<string>(type: "text", nullable: true),
                    StudyDescription = table.Column<string>(type: "text", nullable: true),
                    StudyType = table.Column<string>(type: "text", nullable: true),
                    StudySubtype = table.Column<string>(type: "text", nullable: true),
                    PaperLinked = table.Column<string>(type: "text", nullable: true),
                    PaperTitle = table.Column<string>(type: "text", nullable: true),
                    PaperAuthors = table.Column<string>(type: "text", nullable: true),
                    Affiliation = table.Column<string>(type: "text", nullable: true),
                    PaperJournal = table.Column<string>(type: "text", nullable: true),
                    PaperYear = table.Column<string>(type: "text", nullable: true),
                    PaperDoi = table.Column<string>(type: "text", nullable: true),
                    OpenAccess = table.Column<string>(type: "text", nullable: true),
                    MultiModalityImages = table.Column<string>(type: "text", nullable: true),
                    ImagingModality = table.Column<string>(type: "text", nullable: true),
                    ImagingSubModality = table.Column<string>(type: "text", nullable: true),
                    Radiation = table.Column<string>(type: "text", nullable: true),
                    ImagingCoverage = table.Column<string>(type: "text", nullable: true),
                    ImagingTarget = table.Column<string>(type: "text", nullable: true),
                    Institution = table.Column<string>(type: "text", nullable: true),
                    Pi = table.Column<string>(type: "text", nullable: true),
                    CoPi = table.Column<string>(type: "text", nullable: true),
                    CountryOfInstitution = table.Column<string>(type: "text", nullable: true),
                    ImagingFacility = table.Column<string>(type: "text", nullable: true),
                    EuroBioImagingNode = table.Column<string>(type: "text", nullable: true),
                    CountryOfImagingFacility = table.Column<string>(type: "text", nullable: true),
                    LinkToDataset = table.Column<string>(type: "text", nullable: true),
                    Funding = table.Column<string>(type: "text", nullable: true),
                    FundingAgency = table.Column<string>(type: "text", nullable: true),
                    GrantNumber = table.Column<string>(type: "text", nullable: true),
                    DatasetAccess = table.Column<string>(type: "text", nullable: true),
                    License = table.Column<string>(type: "text", nullable: true),
                    LicenseFile = table.Column<string>(type: "text", nullable: true),
                    DuoDataUsePermission = table.Column<string>(type: "text", nullable: true),
                    DuoDataUseModifier = table.Column<string>(type: "text", nullable: true),
                    DuoInvestigation = table.Column<string>(type: "text", nullable: true),
                    ContactPerson = table.Column<string>(type: "text", nullable: true),
                    NumberOfGroups = table.Column<string>(type: "text", nullable: true),
                    TypesOfGroups = table.Column<string>(type: "text", nullable: true),
                    OverallSampleSize = table.Column<string>(type: "text", nullable: true),
                    DiseaseModel = table.Column<string>(type: "text", nullable: true),
                    OrganOrTissue = table.Column<string>(type: "text", nullable: true),
                    SampleSizeForEachGroup = table.Column<string>(type: "text", nullable: true),
                    PowerCalculation = table.Column<string>(type: "text", nullable: true),
                    InclusionCriteria = table.Column<string>(type: "text", nullable: true),
                    ExclusionCriteria = table.Column<string>(type: "text", nullable: true),
                    Randomization = table.Column<string>(type: "text", nullable: true),
                    Blinding = table.Column<string>(type: "text", nullable: true),
                    ProceduresToKeepTreatmentsBlind = table.Column<string>(type: "text", nullable: true),
                    ProceduresToKeepExperimenterBlind = table.Column<string>(type: "text", nullable: true),
                    OutcomeMeasures = table.Column<string>(type: "text", nullable: true),
                    StatisticalMethods = table.Column<string>(type: "text", nullable: true),
                    Species = table.Column<string>(type: "text", nullable: true),
                    Strain = table.Column<string>(type: "text", nullable: true),
                    ImmuneStatus = table.Column<string>(type: "text", nullable: true),
                    Sex = table.Column<string>(type: "text", nullable: true),
                    Age = table.Column<string>(type: "text", nullable: true),
                    AgeAtStartExperiment = table.Column<string>(type: "text", nullable: true),
                    AgeAtScanningExperimentS = table.Column<string>(type: "text", nullable: true),
                    Weight = table.Column<string>(type: "text", nullable: true),
                    WeightAtStartExperiment = table.Column<string>(type: "text", nullable: true),
                    WeightAtEndExperiment = table.Column<string>(type: "text", nullable: true),
                    Genotype = table.Column<string>(type: "text", nullable: true),
                    GeneticManipulation = table.Column<string>(type: "text", nullable: true),
                    Gene = table.Column<string>(type: "text", nullable: true),
                    SourceOfAnimals = table.Column<string>(type: "text", nullable: true),
                    RegistryNumberOfAnimalAuthorization = table.Column<string>(type: "text", nullable: true),
                    PharmacologicalProceduresInterventionAndControl = table.Column<string>(type: "text", nullable: true),
                    PharmacologicalDrug = table.Column<string>(type: "text", nullable: true),
                    Company = table.Column<string>(type: "text", nullable: true),
                    Formulation = table.Column<string>(type: "text", nullable: true),
                    DrugDose = table.Column<string>(type: "text", nullable: true),
                    Volume = table.Column<string>(type: "text", nullable: true),
                    Concentration = table.Column<string>(type: "text", nullable: true),
                    SiteRouteOfAdministration = table.Column<string>(type: "text", nullable: true),
                    FrequencyOfAdministration = table.Column<string>(type: "text", nullable: true),
                    VehicleOrCarrierSolutionFormulation = table.Column<string>(type: "text", nullable: true),
                    DrugBatchSampleNumber = table.Column<string>(type: "text", nullable: true),
                    BloodSampling = table.Column<string>(type: "text", nullable: true),
                    BloodSamplingMethod = table.Column<string>(type: "text", nullable: true),
                    BloodSampleVolume = table.Column<string>(type: "text", nullable: true),
                    BloodTiming = table.Column<string>(type: "text", nullable: true),
                    SurgicalProceduresIncludingShamSurgery = table.Column<string>(type: "text", nullable: true),
                    DescriptionOfTheSurgicalProcedure = table.Column<string>(type: "text", nullable: true),
                    ReferenceToProtocol = table.Column<string>(type: "text", nullable: true),
                    TargetOrganTissue = table.Column<string>(type: "text", nullable: true),
                    PathogenInfectionInterventionAndControl = table.Column<string>(type: "text", nullable: true),
                    InfectiousType = table.Column<string>(type: "text", nullable: true),
                    InfectiousAgent = table.Column<string>(type: "text", nullable: true),
                    DoseLoad = table.Column<string>(type: "text", nullable: true),
                    SiteAndRouteOfInfection = table.Column<string>(type: "text", nullable: true),
                    TimingOrFrequencyOfInfection = table.Column<string>(type: "text", nullable: true),
                    AnalgesicPlanToRelievePainSufferingAndDistress = table.Column<string>(type: "text", nullable: true),
                    AnalgesicName = table.Column<string>(type: "text", nullable: true),
                    Route = table.Column<string>(type: "text", nullable: true),
                    AnalgesicDose = table.Column<string>(type: "text", nullable: true),
                    AnesthesiaForImaging = table.Column<string>(type: "text", nullable: true),
                    AnesthesiaType = table.Column<string>(type: "text", nullable: true),
                    Duration = table.Column<string>(type: "text", nullable: true),
                    AnesthesiaDrugs = table.Column<string>(type: "text", nullable: true),
                    AnesthesiaDose = table.Column<string>(type: "text", nullable: true),
                    MonitoringRegime = table.Column<string>(type: "text", nullable: true),
                    Euthanasia = table.Column<string>(type: "text", nullable: true),
                    Method = table.Column<string>(type: "text", nullable: true),
                    Histology = table.Column<string>(type: "text", nullable: true),
                    TissuesCollectedPostEuthanasia = table.Column<string>(type: "text", nullable: true),
                    TimingOfCollection = table.Column<string>(type: "text", nullable: true),
                    HistologicalProcedure = table.Column<string>(type: "text", nullable: true),
                    NameOfReagentS = table.Column<string>(type: "text", nullable: true),
                    CatalogueNumber = table.Column<string>(type: "text", nullable: true),
                    LengthOfFixation = table.Column<string>(type: "text", nullable: true),
                    Imaging = table.Column<string>(type: "text", nullable: true),
                    FrequencyOfImaging = table.Column<string>(type: "text", nullable: true),
                    TimingOfImaging = table.Column<string>(type: "text", nullable: true),
                    OverallScanLength = table.Column<string>(type: "text", nullable: true),
                    ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule = table.Column<string>(type: "text", nullable: true),
                    ContrastAgentCommercialDrug = table.Column<string>(type: "text", nullable: true),
                    ContrastAgentChemicalDrug = table.Column<string>(type: "text", nullable: true),
                    ContrastAgentDose = table.Column<string>(type: "text", nullable: true),
                    InjectionVolume = table.Column<string>(type: "text", nullable: true),
                    InjectionTime = table.Column<string>(type: "text", nullable: true),
                    Vehicle = table.Column<string>(type: "text", nullable: true),
                    RouteOfAdministration = table.Column<string>(type: "text", nullable: true),
                    CellLines = table.Column<string>(type: "text", nullable: true),
                    CellLine = table.Column<string>(type: "text", nullable: true),
                    Provenance = table.Column<string>(type: "text", nullable: true),
                    ModifiedCellLine = table.Column<string>(type: "text", nullable: true),
                    TypeOfGeneticModification = table.Column<string>(type: "text", nullable: true),
                    GeneModified = table.Column<string>(type: "text", nullable: true),
                    VerificationAndAuthentication = table.Column<string>(type: "text", nullable: true),
                    CellInjectionRoute = table.Column<string>(type: "text", nullable: true),
                    NumberOfCells = table.Column<string>(type: "text", nullable: true),
                    Reagents = table.Column<string>(type: "text", nullable: true),
                    NameOfReagent = table.Column<string>(type: "text", nullable: true),
                    CatalogueNumbers = table.Column<string>(type: "text", nullable: true),
                    EquipmentAndSoftware = table.Column<string>(type: "text", nullable: true),
                    Manufacturer = table.Column<string>(type: "text", nullable: true),
                    ModelVersionNumber = table.Column<string>(type: "text", nullable: true),
                    FrequencyOfExperimentalProcedures = table.Column<string>(type: "text", nullable: true),
                    TimingOfExperimentalProcedures = table.Column<string>(type: "text", nullable: true),
                    FrequencyOfExperimentalMeasurements = table.Column<string>(type: "text", nullable: true),
                    TimingOfExperimentalMeasurements = table.Column<string>(type: "text", nullable: true),
                    HousingRoom = table.Column<string>(type: "text", nullable: true),
                    DietaryIntervention = table.Column<string>(type: "text", nullable: true),
                    RespirationRate = table.Column<string>(type: "text", nullable: true),
                    BodyTempuratureEtc = table.Column<string>(type: "text", nullable: true),
                    FoodIntakeMeasured = table.Column<string>(type: "text", nullable: true),
                    InstrumentVendor = table.Column<string>(type: "text", nullable: true),
                    InstrumentType = table.Column<string>(type: "text", nullable: true),
                    InstrumentSpecifics = table.Column<string>(type: "text", nullable: true),
                    ImageAcquisitionParameters = table.Column<string>(type: "text", nullable: true),
                    Correction = table.Column<string>(type: "text", nullable: true),
                    RawData = table.Column<string>(type: "text", nullable: true),
                    QaQc = table.Column<string>(type: "text", nullable: true),
                    ImageType = table.Column<string>(type: "text", nullable: true),
                    ImageScale = table.Column<string>(type: "text", nullable: true),
                    FormatCompression = table.Column<string>(type: "text", nullable: true),
                    Dimensions = table.Column<string>(type: "text", nullable: true),
                    OverallNumberOfImages = table.Column<string>(type: "text", nullable: true),
                    FieldOfView = table.Column<string>(type: "text", nullable: true),
                    DimensionExtents = table.Column<string>(type: "text", nullable: true),
                    SizeDescription = table.Column<string>(type: "text", nullable: true),
                    PixelVoxelSizeDescription = table.Column<string>(type: "text", nullable: true),
                    ImageProcessingMethods = table.Column<string>(type: "text", nullable: true),
                    ImageReconstructionAlgorithm = table.Column<string>(type: "text", nullable: true),
                    QualityControl = table.Column<string>(type: "text", nullable: true),
                    ImageSmoothingOrFilteringAlgorithm = table.Column<string>(type: "text", nullable: true),
                    ImageRegistrationAlgorithm = table.Column<string>(type: "text", nullable: true),
                    AiEnhancedAlgorithm = table.Column<string>(type: "text", nullable: true),
                    QcInfo = table.Column<string>(type: "text", nullable: true),
                    Corrections = table.Column<string>(type: "text", nullable: true),
                    SpatialAndTemporalAlignment = table.Column<string>(type: "text", nullable: true),
                    FiducialsUsed = table.Column<string>(type: "text", nullable: true),
                    CoregisteredImages = table.Column<string>(type: "text", nullable: true),
                    TransformationMatrixOtherInfo = table.Column<string>(type: "text", nullable: true),
                    RelatedImagesAndRelationship = table.Column<string>(type: "text", nullable: true),
                    AnalysisResultType = table.Column<string>(type: "text", nullable: true),
                    DataUsedForAnalysis = table.Column<string>(type: "text", nullable: true),
                    AnalysisMethodAndDetails = table.Column<string>(type: "text", nullable: true),
                    FileFormatOfResultFileCsvJsonTxtXlsx = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "text", nullable: true),
                    NcitImaging = table.Column<string>(type: "text", nullable: true),
                    NcitImagingSubmodality = table.Column<string>(type: "text", nullable: true),
                    Doid = table.Column<string>(type: "text", nullable: true),
                    NcitAnatomy = table.Column<string>(type: "text", nullable: true),
                    NcitSpecies = table.Column<string>(type: "text", nullable: true),
                    NcitStrain = table.Column<string>(type: "text", nullable: true),
                    ChebiPharmaco = table.Column<string>(type: "text", nullable: true),
                    ChebiAnesthesia = table.Column<string>(type: "text", nullable: true),
                    ChebiContrastAgentCommercialName = table.Column<string>(type: "text", nullable: true),
                    ChebiContrastAgentChemicalName = table.Column<string>(type: "text", nullable: true),
                    Clo = table.Column<string>(type: "text", nullable: true),
                    NcitGene = table.Column<string>(type: "text", nullable: true),
                    UpdatedYear = table.Column<string>(type: "text", nullable: true),
                    LinkToDataset1 = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_metadata", x => x.dataset_id);
                });

            migrationBuilder.CreateIndex(
                name: "ix_metadata_display_id",
                schema: "public",
                table: "metadata",
                column: "display_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "metadata",
                schema: "public");
        }
    }
}
