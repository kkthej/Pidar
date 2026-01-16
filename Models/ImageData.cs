using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Pidar.Models
{
    public class ImageData
    {
        [Key]
       
        public int DatasetId { get; set; }
        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        [ValidateNever]
        public Dataset Dataset { get; set; } = null!;

        public string? ImageType { get; set; }
        public string? ImageScale { get; set; }
        public string? FormatCompression { get; set; }
        public string? Dimensions { get; set; }
        public string? OverallNumberOfImages { get; set; }
        public string? FieldOfView { get; set; }
        public string? DimensionExtents { get; set; }
        public string? SizeDescription { get; set; }
        public string? PixelVoxelSizeDescription { get; set; }
        public string? ImageProcessingMethods { get; set; }
        public string? ImageReconstructionAlgorithm { get; set; }
        public string? ImageAttenuationCorrection { get; set; }
        public string? QualityControl { get; set; }
        public string? ImageSmoothingOrFilteringAlgorithm { get; set; }
        public string? ImageRegistrationAlgorithm { get; set; }

        // Added from Create.cshtml for consistency
        public string? RegistrationAlgorithms { get; set; }

        public string? AiEnhanced { get; set; }
        public string? AiEnhancedAlgorithm { get; set; }

        public string? QcInfo { get; set; }
        public string? Corrections { get; set; }
    }
}
