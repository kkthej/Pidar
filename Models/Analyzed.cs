using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Pidar.Models
{
    public class Analyzed
    {
        [Key]
        
        public int DatasetId { get; set; }
        // inverse navigation required because your Fluent API uses .WithOne("Dataset")
        [ValidateNever]
        public Dataset Dataset { get; set; } = null!;

        public string? AnalysisResultType { get; set; }
        public string? DataUsedForAnalysis { get; set; }
        public string? AnalysisMethodAndDetails { get; set; }
        public string? FileFormatOfResultFileCsvJsonTxtXlsx { get; set; }
        public string? Status { get; set; }
        public string? UpdatedYear { get; set; }
    }
}
