using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Core.Agents;

public sealed class ResearchOncologyAgent : IResearchOncologyAgent
{
    private readonly IResearchOncologyService _researchService;

    public ResearchOncologyAgent(IResearchOncologyService researchService)
    {
        _researchService = researchService;
    }

    public AgentRole Role => AgentRole.ResearchOncology;
    public string Name => "Research Oncology Agent";

    public bool CanHandle(AgentContext context) => true;

    public async Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var update = await _researchService.GetLatestEvidenceAsync(context.WorkflowRequest.UserInput, cancellationToken);

        return new AgentResponse
        {
            Role = Role,
            Summary = update.PatientFriendlySummary,
            TechnicalSummary = update.TechnicalSummary,
            ConfidenceLevel = 0.55m,
            SuggestedClinicianQuestions = update.SuggestedQuestions,
            OpenQuestions = update.EvidenceGapNotes,
            Citations = update.Citations
        };
    }
}
