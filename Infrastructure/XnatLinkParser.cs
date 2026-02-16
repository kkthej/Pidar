namespace Pidar.Infrastructure;

public static class XnatLinkParser
{
    public static string ExtractProjectId(string linkToDataset)
    {
        var uri = new Uri(linkToDataset);

        // expected: /data/projects/{projectId}
        var parts = uri.AbsolutePath.TrimEnd('/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        var idx = Array.FindLastIndex(parts, p => p.Equals("projects", StringComparison.OrdinalIgnoreCase));
        if (idx < 0 || idx == parts.Length - 1)
            throw new InvalidOperationException($"Cannot parse XNAT project id from: {linkToDataset}");

        return parts[idx + 1];
    }
}
