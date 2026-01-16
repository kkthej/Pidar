using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class ImageCorrelation
    {
        [Key]
       
        public int DatasetId { get; set; }
        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        public Dataset Dataset { get; set; } = null!;
        public string? SpatialAndTemporalAlignment { get; set; }
        public string? FiducialsUsed { get; set; }
        public string? CoregisteredImages { get; set; }
        public string? TransformationMatrixOtherInfo { get; set; }
        public string? RelatedImagesAndRelationship { get; set; }
    }
}
