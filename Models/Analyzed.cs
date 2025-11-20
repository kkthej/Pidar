using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Pidar.Models
{
    public class Analyzed
    {
        [Key]
        [ForeignKey("Dataset")]
        public int DatasetId { get; set; }

        public string? AnalysisResultType { get; set; }
        public string? DataUsedForAnalysis { get; set; }
        public string? AnalysisMethodAndDetails { get; set; }
        public string? FileFormatOfResultFileCsvJsonTxtXlsx { get; set; }
        public string? Status { get; set; }
        public string? UpdatedYear { get; set; }
    }
}
