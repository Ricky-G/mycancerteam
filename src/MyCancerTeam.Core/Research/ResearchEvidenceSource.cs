using MyCancerTeam.Core.Models;

namespace MyCancerTeam.Core.Research;

public sealed class ResearchEvidenceSource
{
    public required CitationMetadata Citation { get; init; }
    public string Summary { get; init; } = string.Empty;
}
