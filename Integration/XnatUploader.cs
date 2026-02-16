using System.Net;
using System.Net.Http.Headers;

namespace Pidar.Integration;

public sealed class XnatUploader : IXnatUploader
{
    private readonly HttpClient _http;
    private readonly IConfiguration _cfg;

    public XnatUploader(HttpClient http, IConfiguration cfg)
    {
        _http = http;
        _cfg = cfg;
    }

    public async Task UploadProjectMetadataAsync(string projectId, string fileName, byte[] content, string contentType)
    {
        var baseUrl = _cfg["Xnat:BaseUrl"]!.TrimEnd('/');

        // 1) Ensure metadata resource exists (409 = already exists => OK)
        var ensureUrl = $"{baseUrl}/data/projects/{Uri.EscapeDataString(projectId)}/resources/metadata";
        using (var ensureReq = new HttpRequestMessage(HttpMethod.Put, ensureUrl))
        {
            var ensureResp = await _http.SendAsync(ensureReq);
            if (!ensureResp.IsSuccessStatusCode && ensureResp.StatusCode != HttpStatusCode.Conflict)
            {
                var body = await ensureResp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"XNAT ensure metadata failed: {(int)ensureResp.StatusCode} {body}");
            }
        }

        // 2) Force replace: DELETE existing file first (ignore 404)
        var deleteUrl =
            $"{baseUrl}/data/projects/{Uri.EscapeDataString(projectId)}/resources/metadata/files/{Uri.EscapeDataString(fileName)}";

        using (var deleteReq = new HttpRequestMessage(HttpMethod.Delete, deleteUrl))
        {
            var delResp = await _http.SendAsync(deleteReq);

            // 404 means "file not there yet" -> OK
            if (!delResp.IsSuccessStatusCode && delResp.StatusCode != HttpStatusCode.NotFound)
            {
                var body = await delResp.Content.ReadAsStringAsync();
                throw new HttpRequestException($"XNAT delete old file failed: {(int)delResp.StatusCode} {body}");
            }
        }

        // 3) Upload (PUT) new file
        var uploadUrl =
            $"{baseUrl}/data/projects/{Uri.EscapeDataString(projectId)}/resources/metadata/files/{Uri.EscapeDataString(fileName)}?inbody=true";

        using var uploadReq = new HttpRequestMessage(HttpMethod.Put, uploadUrl);
        uploadReq.Content = new ByteArrayContent(content);
        uploadReq.Content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

        var uploadResp = await _http.SendAsync(uploadReq);
        if (!uploadResp.IsSuccessStatusCode)
        {
            var body = await uploadResp.Content.ReadAsStringAsync();
            throw new HttpRequestException($"XNAT upload failed: {(int)uploadResp.StatusCode} {body}");
        }
    }
}
