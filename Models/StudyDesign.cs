using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class StudyDesign
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

        public string? StudyDesignBackground { get; set; }
        public string? StudyDescription { get; set; }
        public string? StudyType { get; set; }
        public string? StudySubtype { get; set; }

        
    }
}
