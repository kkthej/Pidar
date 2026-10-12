using Pidar.Models.Xnat;

namespace Pidar.Services.Xnat;

public interface IXnatMultiService
{
    Task<IReadOnlyList<XnatPublicProject>> GetAllPublicProjectsAsync(CancellationToken ct = default);

    /// <summary>True only if the project is in the current public listing for that instance.</summary>
    Task<bool> IsListedPublicProjectAsync(string instanceKey, string projectId, CancellationToken ct = default);
}