using Pidar.Models;
using System.Collections.Generic;

namespace Pidar.Helpers
{
    public interface ICategoryProvider
    {
        Dictionary<string, List<string>> GetCategories();
        
        // NEW: deterministic color for a category title
        string GetColor(string categoryName);
    }

    public class CategoryProvider : ICategoryProvider
    {
        private readonly Dictionary<string, List<string>> _categories =
            new Dictionary<string, List<string>>
            {
                ["Study Design"] = new()
                {
                    "StudyDesign.StudyDesignBackground",
                    "StudyDesign.StudyDescription",
                    "StudyDesign.StudyType",
                    "StudyDesign.StudySubtype"
                },

                ["Publication"] = new()
                {
                    "Publication.PaperLinked","Publication.PaperTitle","Publication.PaperAuthors",
                    "Publication.Affiliation","Publication.PaperJournal","Publication.PaperYear",
                    "Publication.PaperDoi","Publication.OpenAccess"
                },

                ["Study Component"] = new()
                {
                    "StudyComponent.MultiModalityImages","StudyComponent.ImagingModality",
                    "Ontology.NcitImagingModality","StudyComponent.ImagingSubModality",
                    "Ontology.NcitImagingSubmodality","StudyComponent.Radiation",
                    "StudyComponent.ImagingCoverage","Ontology.UberonImagingCoverage","StudyComponent.ImagingTarget"
                },

                ["Dataset Information"] = new()
                {
                    "DatasetInfo.Institution","DatasetInfo.RorCodeOwner","DatasetInfo.Pi",
                    "DatasetInfo.PiOrchid","DatasetInfo.CoPi","DatasetInfo.CountryOfInstitution",
                    "DatasetInfo.ImagingFacility","DatasetInfo.EuroBioImagingNode",
                    "DatasetInfo.CountryOfImagingFacility","DatasetInfo.LinkToDataset",
                    "DatasetInfo.Funding","DatasetInfo.FundingAgency","DatasetInfo.GrantNumber",
                    "DatasetInfo.FunderId","DatasetInfo.DatasetAccess","DatasetInfo.License",
                    "DatasetInfo.LicenseFile","DatasetInfo.DuoDataUsePermission",
                    "DatasetInfo.DuoDataUseModifier","DatasetInfo.DuoInvestigation",
                    "DatasetInfo.ContactPerson"
                },

                ["In Vivo Experimental Parameters"] = new()
                {
                    "InVivo.NumberOfGroups","InVivo.TypesOfGroups","InVivo.OverallSampleSize",
                    "InVivo.AnimalCondition","InVivo.DiseaseModel","Ontology.DoidDiseaseModel",
                    "InVivo.OrganOrTissue","Ontology.UberonOrganOrTissue",
                    "InVivo.SampleSizeForEachGroup","InVivo.PowerCalculation",
                    "InVivo.InclusionCriteria","InVivo.ExclusionCriteria","InVivo.Randomization",
                    "InVivo.Blinding","InVivo.ProceduresToKeepTreatmentsBlind",
                    "InVivo.ProceduresToKeepExperimenterBlind","InVivo.OutcomeMeasures",
                    "InVivo.StatisticalMethods","Ontology.SwoStatisticalMethods","InVivo.Species","Ontology.NcitSpecies",
                    "InVivo.Strain","Ontology.EfoStrain",
                    "InVivo.ImmuneStatus","Ontology.MpImmuneStatus","InVivo.Sex","InVivo.Age",
                    "InVivo.AgeAtStartExperiment","InVivo.AgeAtScanningExperimentS",
                    "InVivo.Weight","InVivo.WeightAtStartExperiment",
                    "InVivo.WeightAtEndExperiment","InVivo.Genotype","Ontology.NcitGenotype",
                    "InVivo.GeneticManipulation","Ontology.NcitGeneticManipulation","InVivo.Gene","Ontology.GoGene",
                    "InVivo.SourceOfAnimals",
                    "InVivo.RegistryNumberOfAnimalAuthorization"
                },

                ["Experimental Procedures"] = new()
                {
                    "Procedures.PharmacologicalProceduresInterventionAndControl",
                    "Procedures.PharmacologicalDrug","Ontology.ChebiDrug","Procedures.Company",
                    "Procedures.Formulation","Procedures.DrugDose","Procedures.Volume",
                    "Procedures.Concentration","Procedures.SiteOrRouteOfAdministration",
                    "Procedures.FrequencyOfAdministration",
                    "Procedures.VehicleOrCarrierSolutionFormulation",
                    "Procedures.DrugOrBatchSampleNumber","Procedures.BloodSampling",
                    "Procedures.BloodSamplingMethod","Procedures.BloodSampleVolume",
                    "Procedures.BloodTiming","Procedures.BloodCollectionTiming",
                    "Procedures.SurgicalProceduresIncludingShamSurgery",
                    "Procedures.DescriptionOfTheSurgicalProcedure",
                    "Procedures.ReferenceToProtocol","Procedures.TargetOrganTissue","Ontology.ObiTargetOrganTissue",
                    "Procedures.PathogenInfectionInterventionAndControl",
                    "Procedures.InfectiousType","Procedures.InfectiousAgent","Procedures.DoseLoad",
                    "Procedures.SiteAndRouteOfInfection",
                    "Procedures.TimingOrFrequencyOfInfection",
                    "Procedures.AnalgesicPlanToRelievePainSufferingAndDistress",
                    "Procedures.AnalgesicName","Ontology.ChebiAnalgesic","Procedures.Route",
                    "Procedures.AnalgesicDose","Procedures.AnesthesiaForImaging",
                    "Procedures.AnesthesiaType","Procedures.Duration",
                    "Procedures.AnesthesiaDrugs","Ontology.ChebiAnestetic","Procedures.AnesthesiaDose",
                    "Procedures.MonitoringRegime","Procedures.Euthanasia","Procedures.Method",
                    "Ontology.ObiEuthanasiaMethod","Procedures.Histology",
                    "Procedures.TissuesCollectedPostEuthanasia","Ontology.UberonTissueExcised",
                    "Procedures.TimingOfCollection","Procedures.TissueDescription","Ontology.MpathHistologicalTissueDescription",
                    "Procedures.PerfusionMethod","Procedures.HistologicalProcedure",
                    "Procedures.NameOfReagentS","Procedures.CatalogueNumber",
                    "Procedures.LengthOfFixation","Procedures.SpecimenThickness",
                    "Procedures.Imaging","Procedures.FrequencyOfImaging",
                    "Procedures.TimingOfImaging","Procedures.OverallScanLength",
                    "Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule",
                    "Procedures.ContrastAgentCommercialDrug",
                    "Procedures.ContrastAgentChemicalDrug",
                    "Ontology.ChebiContrastAgent",
                    "Procedures.ContrastAgentDose","Procedures.InjectionVolume",
                    "Procedures.InjectionTime","Procedures.Vehicle",
                    "Procedures.RouteOfAdministration","Ontology.ObiRouteAdministration",
                    "Procedures.CellLines",
                    "Procedures.CellLine","Ontology.ClCellLineName","Procedures.Provenance",
                    "Procedures.CellCultureMedium","Procedures.ModifiedCellLine",
                    "Procedures.TypeOfGeneticModification","Procedures.GeneModified",
                    "Procedures.VirusLabelledOrModified",
                    "Procedures.VerificationAndAuthentication",
                    "Procedures.CellInjectionRoute","Procedures.CellInjectionProcedure",
                    "Procedures.NumberOfCells","Procedures.Reagents","Procedures.NameOfReagent",
                    "Procedures.CatalogueNumbers","Procedures.EquipmentAndSoftware",
                    "Procedures.Manufacturer","Procedures.ModelVersionNumber",
                    "Procedures.FrequencyOfExperimentalProcedures",
                    "Procedures.TimingOfExperimentalProcedures",
                    "Procedures.FrequencyOfExperimentalMeasurements",
                    "Procedures.TimingOfExperimentalMeasurements","Procedures.HousingRoom",
                    "Procedures.DietaryIntervention","Procedures.RespirationRate",
                    "Procedures.BodyTempuratureEtc","Procedures.FoodIntakeMeasured"
                },

                ["Image Acquisition"] = new()
                {
                    "ImageAcquisition.InstrumentVendor","ImageAcquisition.InstrumentType",
                    "ImageAcquisition.InstrumentSpecifics",
                    "ImageAcquisition.ImageAcquisitionParameters",
                    "ImageAcquisition.Correction","ImageAcquisition.RawData",
                    "ImageAcquisition.QaQc"
                },

                ["Image Data"] = new()
                {
                    "ImageData.ImageType","ImageData.ImageScale",
                    "ImageData.FormatCompression","ImageData.Dimensions",
                    "ImageData.OverallNumberOfImages","ImageData.FieldOfView",
                    "ImageData.DimensionExtents","ImageData.SizeDescription",
                    "ImageData.PixelVoxelSizeDescription",
                    "ImageData.ImageProcessingMethods",
                    "ImageData.ImageReconstructionAlgorithm",
                    "ImageData.QualityControl",
                    "ImageData.ImageSmoothingOrFilteringAlgorithm",
                    "ImageData.ImageRegistrationAlgorithm",
                    "ImageData.AiEnhancedAlgorithm","ImageData.QcInfo",
                    "ImageData.Corrections","ImageData.SpatialAndTemporalAlignment",
                    "ImageData.FiducialsUsed","ImageData.CoregisteredImages",
                    "ImageData.TransformationMatrixOtherInfo",
                    "ImageData.RelatedImagesAndRelationship"
                },

                ["Image Correlation"] = new()
                {
                    "ImageCorrelation.SpatialAndTemporalAlignment",
                    "ImageCorrelation.FiducialsUsed",
                    "ImageCorrelation.CoregisteredImages",
                    "ImageCorrelation.TransformationMatrixOtherInfo",
                    "ImageCorrelation.RelatedImagesAndRelationship"
                },

                ["Analyzed Data"] = new()
                {
                    "Analyzed.AnalysisResultType","Analyzed.DataUsedForAnalysis",
                    "Analyzed.AnalysisMethodAndDetails",
                    "Analyzed.FileFormatOfResultFileCsvJsonTxtXlsx",
                    "Analyzed.Status","Analyzed.UpdatedYear"
                }
            };

        public Dictionary<string, List<string>> GetCategories() => _categories;


        // NEW: category title -> bootstrap color
        private static readonly Dictionary<string, string> _categoryColors =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Study Design"] = "danger",
                ["Publication"] = "secondary",
                ["Study Component"] = "danger",
                ["Dataset Information"] = "secondary",
                ["In Vivo Experimental Parameters"] = "warning",
                ["Experimental Procedures"] = "warning",
                ["Image Acquisition"] = "success",
                ["Image Data"] = "success",
                ["Image Correlation"] = "info",
                ["Analyzed Data"] = "primary"
            };

       

        public string GetColor(string categoryName)
        {
            if (string.IsNullOrWhiteSpace(categoryName))
                return "secondary";

            return _categoryColors.TryGetValue(categoryName.Trim(), out var color)
                ? color
                : "secondary";
        }

    }
}
