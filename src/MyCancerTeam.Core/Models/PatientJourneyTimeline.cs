namespace MyCancerTeam.Core.Models;

public sealed class PatientJourneyTimeline
{
    public IReadOnlyList<string> Milestones { get; init; } = [];
}
