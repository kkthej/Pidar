using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class InVivo
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

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
    }
}
