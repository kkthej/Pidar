using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class Publication
    {
        [Key]
        
        public int DatasetId { get; set; }

        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        public Dataset Dataset { get; set; } = null!;
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
