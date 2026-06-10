namespace MyCancerTeam.Core.Models;

public sealed class GlobalTreatmentOption
{
    public required string Country { get; init; }
    public required string CenterName { get; init; }
    public string OptionSummary { get; init; } = string.Empty;
    public string UncertaintyNotes { get; init; } = string.Empty;
}
