namespace MyCancerTeam.Core.Models;

public sealed class CitationMetadata
{
    public required string SourceName { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public string EvidenceLevel { get; init; } = "unknown";
    public DateOnly? PublishedOn { get; init; }
}
