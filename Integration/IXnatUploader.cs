namespace Pidar.Integration;

public interface IXnatUploader
{
    Task UploadProjectMetadataAsync(
        string projectId,
        string fileName,
        byte[] content,
        string contentType);
}
