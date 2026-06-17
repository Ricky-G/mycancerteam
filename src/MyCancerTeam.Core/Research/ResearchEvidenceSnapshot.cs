namespace MyCancerTeam.Core.Research;

public sealed class ResearchEvidenceSnapshot
{
    public IReadOnlyList<ResearchEvidenceSource> Sources { get; init; } = [];
    public IReadOnlyList<string> Warnings { get; init; } = [];
}
