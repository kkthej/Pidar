using Pidar.Models.Xnat;

namespace Pidar.Services.Xnat;

public interface IXnatMultiService
{
    Task<IReadOnlyList<XnatPublicProject>> GetAllPublicProjectsAsync(CancellationToken ct = default);
}