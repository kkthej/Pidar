using DocumentFormat.OpenXml.Drawing.Charts;
using Microsoft.DotNet.Scaffolding.Shared.CodeModifier.CodeChange;
using NuGet.Protocol.Plugins;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;

namespace Pidar.Models
{
    [Table("dataset")] // explicitly map to the existing table
    public class Dataset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DatasetId { get; set; }

        public int DisplayId { get; set; }

        //Section Study Design
        //Subsection Background
        [Column(TypeName = "text")]
        public string? StudyDesignBackground { get; set; }
        public string? StudyDescription { get; set; }
        public string? StudyType { get; set; }
        public string? StudySubtype { get; set; }


        //Subsection Publication
        public string? PaperLinked { get; set; }
        public string? PaperTitle { get; set; }
        public string? PaperAuthors { get; set; }
        public string? Affiliation { get; set; }
        public string? PaperJournal { get; set; }
        public string? PaperYear { get; set; }
        public string? PaperDoi { get; set; }
        public string? OpenAccess { get; set; }


        //Section Study Component
        //Subsection Imaging Technique
        public string? MultiModalityImages { get; set; }
        public string? ImagingModality { get; set; }
        public string? ImagingSubModality { get; set; }
        public string? Radiation { get; set; }
        public string? ImagingCoverage { get; set; }
        public string? ImagingTarget { get; set; }



        //Subsection Dataset Information

        public string? Institution { get; set; }
        public string? RorCodeOwner { get; set; }
        public string? Pi { get; set; }
        public string? PiOrchid { get; set; }
        public string? CoPi { get; set; }
        public string? CountryOfInstitution { get; set; }
        public string? ImagingFacility { get; set; }
        public string? EuroBioImagingNode { get; set; }
        public string? CountryOfImagingFacility { get; set; }
        public string? LinkToDataset { get; set; }
        public string? Funding { get; set; }
        public string? FundingAgency { get; set; }
        public string? GrantNumber { get; set; }
        public string? FunderId { get; set; }
        public string? DatasetAccess { get; set; }
        public string? License { get; set; }
        public string? LicenseFile { get; set; }
        public string? DuoDataUsePermission { get; set; }
        public string? DuoDataUseModifier { get; set; }
        public string? DuoInvestigation { get; set; }
        public string? ContactPerson { get; set; }



        //Section IVEP-In vivo experimental parameters
        //Subsection Study design
        public string? NumberOfGroups { get; set; }
        public string? TypesOfGroups { get; set; }
        public string? OverallSampleSize { get; set; }
        public string? AnimalCondition { get; set; }
        public string? DiseaseModel { get; set; }
        public string? OrganOrTissue { get; set; }
        public string? SampleSizeForEachGroup { get; set; }
        public string? PowerCalculation { get; set; }
        public string? InclusionCriteria { get; set; }
        public string? ExclusionCriteria { get; set; }
        public string? Randomization { get; set; }
        public string? Blinding { get; set; }
        public string? ProceduresToKeepTreatmentsBlind { get; set; }
        public string? ProceduresToKeepExperimenterBlind { get; set; }
        public string? OutcomeMeasures { get; set; }
        public string? StatisticalMethods { get; set; }
        //Subsection Subject Details
        public string? Species { get; set; }
        public string? Strain { get; set; }
        public string? ImmuneStatus { get; set; }
        public string? Sex { get; set; }
        public string? Age { get; set; }
        public string? AgeAtStartExperiment { get; set; }
        public string? AgeAtScanningExperimentS { get; set; }
        public string? Weight { get; set; }
        public string? WeightAtStartExperiment { get; set; }
        public string? WeightAtEndExperiment { get; set; }
        public string? Genotype { get; set; }
        public string? GeneticManipulation { get; set; }
        public string? Gene { get; set; }
        public string? SourceOfAnimals { get; set; }
        public string? RegistryNumberOfAnimalAuthorization { get; set; }



        //Section Experimental procedures
        //Subsection Procedures
        //a
        public string? PharmacologicalProceduresInterventionAndControl { get; set; }
        [Column(TypeName = "text")]
        public string? PharmacologicalDrug { get; set; }
        public string? Company { get; set; }
        public string? Formulation { get; set; }
        public string? DrugDose { get; set; }
        public string? Volume { get; set; }
        public string? Concentration { get; set; }
        public string? SiteRouteOfAdministration { get; set; }
        public string? FrequencyOfAdministration { get; set; }
        public string? VehicleOrCarrierSolutionFormulation { get; set; }
        public string? DrugBatchSampleNumber { get; set; }
        //b
        public string? BloodSampling { get; set; }
        public string? BloodSamplingMethod { get; set; }
        public string? BloodSampleVolume { get; set; }
        public string? BloodTiming { get; set; }
        public string? BloodCollectionTiming { get; set; }
        //c
        public string? SurgicalProceduresIncludingShamSurgery { get; set; }
        public string? DescriptionOfTheSurgicalProcedure { get; set; }
        public string? ReferenceToProtocol { get; set; }
        public string? TargetOrganTissue { get; set; }
        //d
        public string? PathogenInfectionInterventionAndControl { get; set; }
        public string? InfectiousType { get; set; }
        public string? InfectiousAgent { get; set; }
        public string? DoseLoad { get; set; }
        public string? SiteAndRouteOfInfection { get; set; }
        public string? TimingOrFrequencyOfInfection { get; set; }
        //e
        public string? AnalgesicPlanToRelievePainSufferingAndDistress { get; set; }
        public string? AnalgesicName { get; set; }
        public string? Route { get; set; }
        public string? AnalgesicDose { get; set; }
        //f
        public string? AnesthesiaForImaging { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? Duration { get; set; }
        public string? AnesthesiaDrugs { get; set; }
        public string? AnesthesiaDose { get; set; }
        public string? MonitoringRegime { get; set; }
        //g
        public string? Euthanasia { get; set; }
        public string? Method { get; set; }
        //h
        public string? Histology { get; set; }
        public string? TissuesCollectedPostEuthanasia { get; set; }
        public string? TimingOfCollection { get; set; }
        public string? TissueDescription { get; set; }
        public string? TissuePerfused { get; set; }
        public string? PerfusionMethod { get; set; }
        public string? HistologicalProcedure { get; set; }
        public string? NameOfReagentS { get; set; }
        public string? CatalogueNumber { get; set; }
        public string? LengthOfFixation { get; set; }
        public string? SpecimenThickness { get; set; }
        //i
        public string? Imaging { get; set; }
        public string? FrequencyOfImaging { get; set; }
        [Column(TypeName = "text")]
        public string? TimingOfImaging { get; set; }
        public string? OverallScanLength { get; set; }
        public string? ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule { get; set; }
        public string? ContrastAgentCommercialDrug { get; set; }
        public string? ContrastAgentChemicalDrug { get; set; }
        public string? ContrastAgentDose { get; set; }
        public string? InjectionVolume { get; set; }
        public string? InjectionTime { get; set; }
        public string? Vehicle { get; set; }
        public string? RouteOfAdministration { get; set; }

        //Subsection Resources
        //a
        public string? CellLines { get; set; }
        public string? CellLine { get; set; }
        public string? Provenance { get; set; }
        public string? CellCultureMedium { get; set; }
        public string? ModifiedCellLine { get; set; }
        public string? TypeOfGeneticModification { get; set; }
        public string? GeneModified { get; set; }
        public string? VirusLabelledOrModified { get; set; }
        public string? VerificationAndAuthentication { get; set; }
        public string? CellInjectionRoute { get; set; }
        public string? CellInjectionProcedure { get; set; }
        public string? NumberOfCells { get; set; }
        //b
        public string? Reagents { get; set; }
        public string? NameOfReagent { get; set; }
        public string? CatalogueNumbers { get; set; }
        //c
        public string? EquipmentAndSoftware { get; set; }
        public string? Manufacturer { get; set; }
        public string? ModelVersionNumber { get; set; }


        //Subsection Additional Information
        public string? FrequencyOfExperimentalProcedures { get; set; }
        public string? TimingOfExperimentalProcedures { get; set; }
        public string? FrequencyOfExperimentalMeasurements { get; set; }
        public string? TimingOfExperimentalMeasurements { get; set; }
        public string? HousingRoom { get; set; }
        public string? DietaryIntervention { get; set; }
        public string? RespirationRate { get; set; }
        public string? BodyTempuratureEtc { get; set; }
        public string? FoodIntakeMeasured { get; set; }



        //Section Image acquisition
        public string? InstrumentVendor { get; set; }
        public string? InstrumentType { get; set; }
        public string? InstrumentSpecifics { get; set; }
        public string? ImageAcquisitionParameters { get; set; }
        public string? Correction { get; set; }
        public string? RawData { get; set; }
        public string? QaQc { get; set; }



        //Section Image Data
        public string? ImageType { get; set; }
        public string? ImageScale { get; set; }
        public string? FormatCompression { get; set; }
        public string? Dimensions { get; set; }
        public string? OverallNumberOfImages { get; set; }
        public string? FieldOfView { get; set; }
        public string? DimensionExtents { get; set; }
        public string? SizeDescription { get; set; }
        public string? PixelVoxelSizeDescription { get; set; }
        public string? ImageProcessingMethods { get; set; }
        public string? ImageReconstructionAlgorithm { get; set; }
        public string? ImageAttenuationCorrection { get; set; }
        public string? ImageSmoothingOrFilteringAlgorithm { get; set; }
        public string? ImageRegistrationAlgorithm { get; set; }
        public string? RegistrationAlgorithms { get; set; }
        public string? AiEnhanced { get; set; }
        public string? AiEnhancedAlgorithm { get; set; }
        public string? QualityControl { get; set; }
        public string? QcInfo { get; set; }
        public string? Corrections { get; set; }

        //Section IMAGE CORRELATION
        public string? SpatialAndTemporalAlignment { get; set; }
        public string? FiducialsUsed { get; set; }
        public string? CoregisteredImages { get; set; }
        public string? TransformationMatrixOtherInfo { get; set; }
        public string? RelatedImagesAndRelationship { get; set; }


        //Section ANALYSED DATA

        public string? AnalysisResultType { get; set; }
        public string? DataUsedForAnalysis { get; set; }
        public string? AnalysisMethodAndDetails { get; set; }
        public string? FileFormatOfResultFileCsvJsonTxtXlsx { get; set; }
        public string? Status { get; set; }




        //Section Ontologies and Standards
        public string? NcitImaging { get; set; }

        public string? NcitImagingSubmodality { get; set; }

        public string? Doid { get; set; }

        public string? NcitAnatomy { get; set; }

        public string? NcitSpecies { get; set; }

        public string? NcitStrain { get; set; }

        public string? ChebiPharmaco { get; set; }

        public string? ChebiAnesthesia { get; set; }

        public string? ChebiContrastAgentCommercialName { get; set; }

        public string? ChebiContrastAgentChemicalName { get; set; }

        public string? Clo { get; set; }

        public string? NcitGene { get; set; }


        //Section Dataset Versioning

        public string? UpdatedYear { get; set; }

       

        public Dataset()
        {

        }
    }
}
