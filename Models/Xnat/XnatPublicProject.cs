namespace Pidar.Models.Xnat;

public sealed record XnatPublicProject(
    string InstanceKey,
    string InstanceName,
    string InstanceBaseUrl,
    string Id,
    string? Description,
    string? PiFullName,
    string Uri
);