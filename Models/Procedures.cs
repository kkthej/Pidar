using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class Procedures
    {

        [Key]
       
        public int DatasetId { get; set; }
        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        public Dataset Dataset { get; set; } = null!;

        // Pharmacological
        public string? PharmacologicalProceduresInterventionAndControl { get; set; }
        public string? PharmacologicalDrug { get; set; }
        public string? Company { get; set; }
        public string? Formulation { get; set; }
        public string? DrugDose { get; set; }
        public string? Volume { get; set; }
        public string? Concentration { get; set; }
        public string? SiteOrRouteOfAdministration { get; set; }
        public string? FrequencyOfAdministration { get; set; }
        public string? VehicleOrCarrierSolutionFormulation { get; set; }
        public string? DrugOrBatchSampleNumber { get; set; }

        // Blood sampling
        public string? BloodSampling { get; set; }
        public string? BloodSamplingMethod { get; set; }
        public string? BloodSampleVolume { get; set; }
        public string? BloodTiming { get; set; }
        public string? BloodCollectionTiming { get; set; }

        // Surgery
        public string? SurgicalProceduresIncludingShamSurgery { get; set; }
        public string? DescriptionOfTheSurgicalProcedure { get; set; }
        public string? ReferenceToProtocol { get; set; }
        public string? TargetOrganTissue { get; set; }

        // Infection
        public string? PathogenInfectionInterventionAndControl { get; set; }
        public string? InfectiousType { get; set; }
        public string? InfectiousAgent { get; set; }
        public string? DoseLoad { get; set; }
        public string? SiteAndRouteOfInfection { get; set; }
        public string? TimingOrFrequencyOfInfection { get; set; }

        // Analgesia
        public string? AnalgesicPlanToRelievePainSufferingAndDistress { get; set; }
        public string? AnalgesicName { get; set; }
        public string? Route { get; set; }
        public string? AnalgesicDose { get; set; }

        // Anesthesia
        public string? AnesthesiaForImaging { get; set; }
        public string? AnesthesiaType { get; set; }
        public string? Duration { get; set; }
        public string? AnesthesiaDrugs { get; set; }
        public string? AnesthesiaDose { get; set; }
        public string? MonitoringRegime { get; set; }

        // Euthanasia
        public string? Euthanasia { get; set; }
        public string? Method { get; set; }
        public string? Histology { get; set; }

        // Histology details
        public string? TissuesCollectedPostEuthanasia { get; set; }
        public string? TimingOfCollection { get; set; }
        public string? TissueDescription { get; set; }
        public string? TissuePerfused { get; set; }
        public string? PerfusionMethod { get; set; }
        public string? HistologicalProcedure { get; set; }

        // Reagents
        public string? NameOfReagentS { get; set; }
        public string? CatalogueNumber { get; set; }
        public string? LengthOfFixation { get; set; }
        public string? SpecimenThickness { get; set; }

        // Imaging procedures
        public string? Imaging { get; set; }
        public string? FrequencyOfImaging { get; set; }
        public string? TimingOfImaging { get; set; }
        public string? OverallScanLength { get; set; }

        // Contrast agents
        public string? ContrastAgentOrRadioIsotopeOrChallengeWithGasMolecule { get; set; }
        public string? ContrastAgentCommercialDrug { get; set; }
        public string? ContrastAgentChemicalDrug { get; set; }
        public string? ContrastAgentDose { get; set; }
        public string? InjectionVolume { get; set; }
        public string? InjectionTime { get; set; }
        public string? Vehicle { get; set; }
        public string? RouteOfAdministration { get; set; }

        // Cell lines
        public string? CellLines { get; set; }
        public string? CellLine { get; set; }
        public string? Provenance { get; set; }
        public string? CellCultureMedium { get; set; }
        public string? ModifiedCellLine { get; set; }
        public string? TypeOfGeneticModification { get; set; }
        public string? GeneModified { get; set; }
        public string? VirusLabelledOrModified { get; set; }
        public string? VerificationAndAuthentication { get; set; }

        // Cell injections
        public string? CellInjectionRoute { get; set; }
        public string? CellInjectionProcedure { get; set; }
        public string? NumberOfCells { get; set; }

        // Equipment & software
        public string? Reagents { get; set; }
        public string? NameOfReagent { get; set; }
        public string? CatalogueNumbers { get; set; }
        public string? EquipmentAndSoftware { get; set; }
        public string? Manufacturer { get; set; }
        public string? ModelVersionNumber { get; set; }

        // Experimental conditions
        public string? FrequencyOfExperimentalProcedures { get; set; }
        public string? TimingOfExperimentalProcedures { get; set; }
        public string? FrequencyOfExperimentalMeasurements { get; set; }
        public string? TimingOfExperimentalMeasurements { get; set; }
        public string? HousingRoom { get; set; }
        public string? DietaryIntervention { get; set; }
        public string? RespirationRate { get; set; }
        public string? BodyTempuratureEtc { get; set; }
        public string? FoodIntakeMeasured { get; set; }
    }
}
