namespace MyCancerTeam.Core.Agents;

public sealed class PatientOwnerAgent : ISpecialistAgent
{
    public AgentRole Role => AgentRole.PatientOwner;
    public string Name => "Patient / Support Person Owner Agent";

    public bool CanHandle(AgentContext context) => true;

    public Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentResponse
        {
            Role = Role,
            Summary = "Patient priorities, constraints, symptoms, and practical realities captured for team consideration.",
            TechnicalSummary = "Updates emphasize preferences, daily-function concerns, caregiver constraints, and unresolved concerns.",
            ConfidenceLevel = 0.70m,
            OpenQuestions = ["Confirm missing home support and travel constraints before major decisions."]
        });
    }
}
