using MyCancerTeam.Core.Models;

namespace MyCancerTeam.Core.Agents;

public sealed class AgentResponse
{
    public required AgentRole Role { get; init; }
    public required string Summary { get; init; }
    public string TechnicalSummary { get; init; } = string.Empty;
    public decimal ConfidenceLevel { get; init; }
    public IReadOnlyList<string> SuggestedClinicianQuestions { get; init; } = [];
    public IReadOnlyList<string> OpenQuestions { get; init; } = [];
    public IReadOnlyList<string> EngagedAgents { get; init; } = [];
    public IReadOnlyList<CitationMetadata> Citations { get; init; } = [];
}
