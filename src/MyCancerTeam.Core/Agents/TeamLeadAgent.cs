using System.Text;
using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Core.Agents;

public sealed class TeamLeadAgent : ITeamLeadAgent
{
    private const decimal MinimumConfidenceLevel = 0.20m;
    private const decimal StandardConfidenceLevel = 0.60m;

    private readonly IAgentRegistry _agentRegistry;
    private readonly WorkflowRouter _workflowRouter;

    public TeamLeadAgent(IAgentRegistry agentRegistry, WorkflowRouter workflowRouter)
    {
        _agentRegistry = agentRegistry;
        _workflowRouter = workflowRouter;
    }

    public AgentRole Role => AgentRole.TeamLead;
    public string Name => "Team Lead Agent";

    public bool CanHandle(AgentContext context) => true;

    public Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
        => CoordinateAsync(context.WorkflowRequest, context.SharedNotes, cancellationToken);

    public async Task<AgentResponse> CoordinateAsync(WorkflowRequest request, string sharedNotes, CancellationToken cancellationToken = default)
    {
        var roles = _workflowRouter.GetRecommendedRoles(request);
        var specialistResponses = new List<AgentResponse>();

        foreach (var role in roles)
        {
            var agent = _agentRegistry.Get(role);
            if (agent is null)
            {
                continue;
            }

            var response = await agent.RespondAsync(new AgentContext
            {
                WorkflowRequest = request,
                SharedNotes = sharedNotes
            }, cancellationToken);

            specialistResponses.Add(response);
        }

        var summaryBuilder = new StringBuilder();
        summaryBuilder.AppendLine("Team synthesis prepared. Facts, assumptions, and unknowns are separated for clinician discussion.");

        foreach (var response in specialistResponses)
        {
            summaryBuilder.AppendLine($"- {response.Role}: {response.Summary}");
        }

        var openQuestions = specialistResponses
            .SelectMany(r => r.OpenQuestions)
            .Distinct()
            .ToList();

        var suggestions = specialistResponses
            .SelectMany(r => r.SuggestedClinicianQuestions)
            .Distinct()
            .ToList();

        var citations = specialistResponses
            .SelectMany(r => r.Citations)
            .ToList();

        return new AgentResponse
        {
            Role = Role,
            Summary = summaryBuilder.ToString().Trim(),
            TechnicalSummary = "Consolidated specialist viewpoints with tracked uncertainties and unresolved issues.",
            ConfidenceLevel = specialistResponses.Count == 0 ? MinimumConfidenceLevel : StandardConfidenceLevel,
            OpenQuestions = openQuestions,
            SuggestedClinicianQuestions = suggestions,
            Citations = citations
        };
    }
}
