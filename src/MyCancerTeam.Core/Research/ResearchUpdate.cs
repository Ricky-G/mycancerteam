using MyCancerTeam.Core.Models;

namespace MyCancerTeam.Core.Research;

public sealed class ResearchUpdate
{
    public string PatientFriendlySummary { get; init; } = string.Empty;
    public string TechnicalSummary { get; init; } = string.Empty;
    public IReadOnlyList<string> SuggestedQuestions { get; init; } = [];
    public IReadOnlyList<string> EvidenceGapNotes { get; init; } = [];
    public IReadOnlyList<CitationMetadata> Citations { get; init; } = [];
}
