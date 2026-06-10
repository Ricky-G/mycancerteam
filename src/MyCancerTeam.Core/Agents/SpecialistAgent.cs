namespace MyCancerTeam.Core.Agents;

public sealed class SpecialistAgent : ISpecialistAgent
{
    public SpecialistAgent(AgentRole role, string name)
    {
        Role = role;
        Name = name;
    }

    public AgentRole Role { get; }
    public string Name { get; }

    public bool CanHandle(AgentContext context) => true;

    public Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentResponse
        {
            Role = Role,
            Summary = $"{Name} review prepared. Distinguish facts, assumptions, and uncertainties with clinician follow-up.",
            TechnicalSummary = "Placeholder specialist summary with risks, tradeoffs, and missing information.",
            ConfidenceLevel = 0.45m,
            SuggestedClinicianQuestions =
            [
                "What additional patient-specific information should be collected before decision-making?",
                "Which urgent red flags should trigger immediate clinical escalation?"
            ],
            OpenQuestions = ["Pending specialist-specific evidence review."]
        });
    }
}
