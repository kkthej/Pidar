using System.Collections.Generic;

namespace Pidar.Helpers
{
    public interface ICategoryProvider
    {
        Dictionary<string, List<string>> GetCategories();
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
                    "Ontology.NcitImaging","StudyComponent.ImagingSubModality",
                    "Ontology.NcitImagingSubmodality","StudyComponent.Radiation",
                    "StudyComponent.ImagingCoverage","StudyComponent.ImagingTarget"
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
                    "InVivo.AnimalCondition","InVivo.DiseaseModel","Ontology.Doid",
                    "InVivo.OrganOrTissue","Ontology.NcitAnatomy",
                    "InVivo.SampleSizeForEachGroup","InVivo.PowerCalculation",
                    "InVivo.InclusionCriteria","InVivo.ExclusionCriteria","InVivo.Randomization",
                    "InVivo.Blinding","InVivo.ProceduresToKeepTreatmentsBlind",
                    "InVivo.ProceduresToKeepExperimenterBlind","InVivo.OutcomeMeasures",
                    "InVivo.StatisticalMethods","InVivo.Species","Ontology.NcitSpecies",
                    "InVivo.Strain","Ontology.NcitStrain",
                    "InVivo.ImmuneStatus","InVivo.Sex","InVivo.Age",
                    "InVivo.AgeAtStartExperiment","InVivo.AgeAtScanningExperimentS",
                    "InVivo.Weight","InVivo.WeightAtStartExperiment",
                    "InVivo.WeightAtEndExperiment","InVivo.Genotype",
                    "InVivo.GeneticManipulation","InVivo.Gene","InVivo.SourceOfAnimals",
                    "InVivo.RegistryNumberOfAnimalAuthorization"
                },

                ["Experimental Procedures"] = new()
                {
                    "Procedures.PharmacologicalProceduresInterventionAndControl",
                    "Procedures.PharmacologicalDrug","Ontology.ChebiPharmaco","Procedures.Company",
                    "Procedures.Formulation","Procedures.DrugDose","Procedures.Volume",
                    "Procedures.Concentration","Procedures.SiteOrRouteOfAdministration",
                    "Procedures.FrequencyOfAdministration",
                    "Procedures.VehicleOrCarrierSolutionFormulation",
                    "Procedures.DrugOrBatchSampleNumber","Procedures.BloodSampling",
                    "Procedures.BloodSamplingMethod","Procedures.BloodSampleVolume",
                    "Procedures.BloodTiming","Procedures.BloodCollectionTiming",
                    "Procedures.SurgicalProceduresIncludingShamSurgery",
                    "Procedures.DescriptionOfTheSurgicalProcedure",
                    "Procedures.ReferenceToProtocol","Procedures.TargetOrganTissue",
                    "Procedures.PathogenInfectionInterventionAndControl",
                    "Procedures.InfectiousType","Procedures.InfectiousAgent","Procedures.DoseLoad",
                    "Procedures.SiteAndRouteOfInfection",
                    "Procedures.TimingOrFrequencyOfInfection",
                    "Procedures.AnalgesicPlanToRelievePainSufferingAndDistress",
                    "Procedures.AnalgesicName","Ontology.ChebiAnesthesia","Procedures.Route",
                    "Procedures.AnalgesicDose","Procedures.AnesthesiaForImaging",
                    "Procedures.AnesthesiaType","Procedures.Duration",
                    "Procedures.AnesthesiaDrugs","Procedures.AnesthesiaDose",
                    "Procedures.MonitoringRegime","Procedures.Euthanasia","Procedures.Method",
                    "Procedures.Histology","Procedures.TissuesCollectedPostEuthanasia",
                    "Procedures.TimingOfCollection","Procedures.TissueDescription",
                    "Procedures.PerfusionMethod","Procedures.HistologicalProcedure",
                    "Procedures.NameOfReagentS","Procedures.CatalogueNumber",
                    "Procedures.LengthOfFixation","Procedures.SpecimenThickness",
                    "Procedures.Imaging","Procedures.FrequencyOfImaging",
                    "Procedures.TimingOfImaging","Procedures.OverallScanLength",
                    "Procedures.ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule",
                    "Procedures.ContrastAgentCommercialDrug",
                    "Ontology.ChebiContrastAgentCommercialName",
                    "Procedures.ContrastAgentChemicalDrug",
                    "Ontology.ChebiContrastAgentChemicalName",
                    "Procedures.ContrastAgentDose","Procedures.InjectionVolume",
                    "Procedures.InjectionTime","Procedures.Vehicle",
                    "Procedures.RouteOfAdministration","Procedures.CellLines","Ontology.Clo",
                    "Procedures.CellLine","Procedures.Provenance",
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
                },

                ["Ontology Terms"] = new()
                {
                    "Ontology.NcitGene"
                }
            };

        public Dictionary<string, List<string>> GetCategories() => _categories;
    }
}
