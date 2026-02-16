using Microsoft.EntityFrameworkCore;
using Pidar.Data;
using Pidar.Infrastructure;
using Pidar.Integration;
using Pidar.Services;

namespace Pidar.Jobs;

public sealed class DatasetXnatSyncJob
{
    private readonly PidarDbContext _db;
    private readonly IXnatUploader _xnat;
    private readonly IDatasetExportService _export;
    private readonly ILogger<DatasetXnatSyncJob> _logger;

    public DatasetXnatSyncJob(
        PidarDbContext db,
        IXnatUploader xnat,
        IDatasetExportService export,
        ILogger<DatasetXnatSyncJob> logger)
    {
        _db = db;
        _xnat = xnat;
        _export = export;
        _logger = logger;
    }

    public async Task SyncDatasetAsync(int datasetId)
    {
        var link = await _db.Datasets
            .AsNoTracking()
            .Where(d => d.DatasetId == datasetId)
            .Select(d => d.DatasetInfo!.LinkToDataset)
            .FirstOrDefaultAsync();

        if (string.IsNullOrWhiteSpace(link))
        {
            _logger.LogInformation("XNAT sync skipped for DatasetId={DatasetId} (LinkToDataset empty)", datasetId);
            return;
        }

        var projectId = XnatLinkParser.ExtractProjectId(link);

        // your rule
        var fileName = $"{projectId}.json";

        // “same JSON as DownloadController”
        var bytes = await _export.ExportDatasetJsonAsync(datasetId);

        await _xnat.UploadProjectMetadataAsync(
            projectId: projectId,
            fileName: fileName,
            content: bytes,
            contentType: "application/json");
    }
}
