using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    [Table("dataset")]
    public class Dataset
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int DatasetId { get; set; }

        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public int DisplayId { get; set; }


        // Navigation properties (1:1)
        public StudyDesign? StudyDesign { get; set; }
        public Publication? Publication { get; set; }
        public StudyComponent? StudyComponent { get; set; }
        public DatasetInfo? DatasetInfo { get; set; }
        public InVivo? InVivo { get; set; }
        public Procedures? Procedures { get; set; }
        public ImageAcquisition? ImageAcquisition { get; set; }
        public ImageData? ImageData { get; set; }
        public ImageCorrelation? ImageCorrelation { get; set; }
        public Analyzed? Analyzed { get; set; }
        public Ontology? Ontology { get; set; }

        public Dataset() { }
    }
}
