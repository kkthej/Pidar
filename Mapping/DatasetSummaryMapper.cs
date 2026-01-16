using Pidar.Models;
using Pidar.Models.Summaries;

namespace Pidar.Mapping;

public static class DatasetSummaryMapper
{
    public static DatasetSummary FromDataset(Dataset dataset)
    {
        ArgumentNullException.ThrowIfNull(dataset);

        return new DatasetSummary
        {
            DisplayId = dataset.DisplayId,
            Species = dataset.InVivo?.Species,
            OrganOrTissue = dataset.InVivo?.OrganOrTissue,
            DiseaseModel = dataset.InVivo?.DiseaseModel,
            SampleSize = dataset.InVivo?.OverallSampleSize,
            ImagingModality = dataset.StudyComponent?.ImagingModality
        };
    }
}
