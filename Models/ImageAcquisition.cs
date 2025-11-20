using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class ImageAcquisition
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

        public string? InstrumentVendor { get; set; }
        public string? InstrumentType { get; set; }
        public string? InstrumentSpecifics { get; set; }
        public string? ImageAcquisitionParameters { get; set; }
        public string? Correction { get; set; }
        public string? RawData { get; set; }

        // Matches Create/Edit form exactly (case-sensitive)
        public string? QaQc { get; set; }
    }
}
