using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class StudyComponent
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

        public string? MultiModalityImages { get; set; }
        public string? ImagingModality { get; set; }
        public string? ImagingSubModality { get; set; }
        public string? Radiation { get; set; }
        public string? ImagingCoverage { get; set; }
        public string? ImagingTarget { get; set; }

        
    }
}
