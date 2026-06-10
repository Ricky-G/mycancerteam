namespace MyCancerTeam.Core.Models;

public sealed class ResearchSource
{
    public required string Name { get; init; }
    public required string Url { get; init; }
    public string Notes { get; init; } = string.Empty;
}
