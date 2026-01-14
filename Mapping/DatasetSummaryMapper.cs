using Pidar.Models;
using Pidar.Models.Summaries;

namespace Pidar.Mapping;

public static class DatasetSummaryMapper
{
    public static DatasetSummary FromDataset(Dataset ds) => new()
    {
       
        DisplayId = ds.DisplayId,
        Species = ds.InVivo?.Species,
        OrganOrTissue = ds.InVivo?.OrganOrTissue,
        DiseaseModel = ds.InVivo?.DiseaseModel,
        SampleSize = ds.InVivo?.OverallSampleSize,
        ImagingModality = ds.StudyComponent?.ImagingModality
    };
}
