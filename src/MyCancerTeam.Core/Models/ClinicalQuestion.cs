namespace MyCancerTeam.Core.Models;

public sealed class ClinicalQuestion
{
    public required string Question { get; init; }
    public string Rationale { get; init; } = string.Empty;
}
