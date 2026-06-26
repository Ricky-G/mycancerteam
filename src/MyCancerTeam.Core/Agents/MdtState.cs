namespace MyCancerTeam.Core.Agents;

/// <summary>
/// Structured snapshot of the current multi-disciplinary team (MDT) state.
/// Persisted as JSON so that subsequent turns can read it back without regex-parsing the rendered Markdown summary.
/// </summary>
public sealed class MdtState
{
    public string CurrentDiagnosis { get; init; } = string.Empty;
    public string CurrentTreatment { get; init; } = string.Empty;

    /// <summary>Combined open questions and suggested clinician questions from the last MDT response.</summary>
    public IReadOnlyList<string> NextSteps { get; init; } = [];

    /// <summary>Accumulated names of all agents that have contributed across all turns.</summary>
    public IReadOnlyList<string> EngagedAgents { get; init; } = [];
}
