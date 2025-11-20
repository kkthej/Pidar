using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class ImageCorrelation
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

        public string? SpatialAndTemporalAlignment { get; set; }
        public string? FiducialsUsed { get; set; }
        public string? CoregisteredImages { get; set; }
        public string? TransformationMatrixOtherInfo { get; set; }
        public string? RelatedImagesAndRelationship { get; set; }
    }
}
