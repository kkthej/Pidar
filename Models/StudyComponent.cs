using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;


namespace Pidar.Models
{
    public class StudyComponent
    {
        [Key]
        
        public int DatasetId { get; set; }

        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        [ValidateNever]
        public Dataset Dataset { get; set; } = null!;
        public string? MultiModalityImages { get; set; }
        public string? ImagingModality { get; set; }
        public string? ImagingSubModality { get; set; }
        public string? Radiation { get; set; }
        public string? ImagingCoverage { get; set; }
        public string? ImagingTarget { get; set; }

        
    }
}
