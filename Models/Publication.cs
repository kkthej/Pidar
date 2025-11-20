using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class Publication
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

        public string? PaperLinked { get; set; }
        public string? PaperTitle { get; set; }
        public string? PaperAuthors { get; set; }
        public string? Affiliation { get; set; }
        public string? PaperJournal { get; set; }
        public string? PaperYear { get; set; }
        public string? PaperDoi { get; set; }
        public string? OpenAccess { get; set; }

        
    }
}
