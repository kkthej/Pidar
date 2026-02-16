namespace Pidar.Exports;

public static class ExportLabelMap
{
    public static readonly Dictionary<string, string> Map = new()
    {
        { "DisplayId", "Dataset ID" },
        { "OverallSampleSize", "Sample size" },
        { "OrganOrTissue", "Organ/Tissue" },
        { "DiseaseModel", "Disease model" },
        { "ImagingModality", "Imaging modality" },
        { "PaperTitle", "Paper title" },
        { "PaperAuthors", "Authors" },
        { "PaperJournal", "Journal" },
        { "PaperYear", "Year" },
        { "PaperDoi", "DOI" }
    };
}
