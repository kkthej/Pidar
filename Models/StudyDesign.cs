using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class StudyDesign
    {
        [Key]
        
        public int DatasetId { get; set; }

        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        public Dataset Dataset { get; set; } = null!;

        public string? StudyDesignBackground { get; set; }
        public string? StudyDescription { get; set; }
        public string? StudyType { get; set; }
        public string? StudySubtype { get; set; }

        
    }
}
