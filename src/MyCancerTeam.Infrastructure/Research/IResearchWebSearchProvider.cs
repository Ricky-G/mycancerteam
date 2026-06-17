using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public interface IResearchWebSearchProvider
{
    Task<ResearchEvidenceSnapshot> SearchAsync(string query, CancellationToken cancellationToken = default);
}
