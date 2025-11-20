using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class Ontology
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

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
    }
}
