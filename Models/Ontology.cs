using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class Ontology
    {
        [Key]
        
        public int DatasetId { get; set; }
        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        public Dataset Dataset { get; set; } = null!;

        // RENAMED / EXISTING
        public string? NcitImagingModality { get; set; }         // was NcitImaging
        public string? NcitImagingSubmodality { get; set; }      // same
        public string? UberonOrganOrTissue { get; set; }         // was NcitAnatomy
        public string? DoidDiseaseModel { get; set; }            // was Doid
        public string? NcitSpecies { get; set; }                 // same
        public string? EfoStrain { get; set; }                   // was NcitStrain
        public string? GoGene { get; set; }                      // was NcitGene
        public string? ChebiDrug { get; set; }                   // was ChebiPharmaco
        public string? ChebiAnesthetic { get; set; }             // was ChebiAnesthesia
        public string? ChebiContrastAgent { get; set; }          // was ChebiContrastAgentChemicalName
        public string? ClCellLineName { get; set; }              // was Clo

        // NEW
        public string? UberonImagingCoverage { get; set; }
        public string? SwoStatisticalMethods { get; set; }
        public string? MpImmuneStatus { get; set; }
        public string? NcitGenotype { get; set; }
        public string? NcitGeneticManipulation { get; set; }
        public string? ObiTargetOrganTissue { get; set; }
        public string? ChebiAnalgesic { get; set; }
        public string? ObiEuthanasiaMethod { get; set; }
        public string? UberonTissueExcised { get; set; }
        public string? MpathHistologicalTissueDescription { get; set; }
        public string? ObiRouteAdministration { get; set; }

        // navigation (optional; you already map 1:1 from Dataset side)
        
    }
}
