namespace Pidar.Models.Summaries
{
    public sealed class DatasetSummary { 
       
        public int DisplayId { get; init; } 
        public string? Species { get; init; } 
        public string? OrganOrTissue { get; init; } 
        public string? DiseaseModel { get; init; } 
        public string? ImagingModality { get; init; } 
        public string? SampleSize { get; init; } }
}
