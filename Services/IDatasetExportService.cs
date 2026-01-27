namespace Pidar.Services;

public interface IDatasetExportService
{
    Task<byte[]> ExportDatasetJsonAsync(int datasetId);
    Task<byte[]> ExportDatasetCsvAsync(int datasetId);
}
