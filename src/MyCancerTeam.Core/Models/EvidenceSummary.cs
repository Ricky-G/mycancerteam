namespace MyCancerTeam.Core.Models;

public sealed class EvidenceSummary
{
    public string EstablishedCare { get; init; } = string.Empty;
    public string GuidelineBacked { get; init; } = string.Empty;
    public string EmergingEvidence { get; init; } = string.Empty;
    public string Uncertainties { get; init; } = string.Empty;
}
