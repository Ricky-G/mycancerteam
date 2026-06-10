using System.Text;
using Microsoft.Agents.AI.Workflows;
using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Core.Agents;

public sealed class TeamLeadAgent : ITeamLeadAgent
{
    private const decimal MinimumConfidenceLevel = 0.20m;
    private const decimal StandardConfidenceLevel = 0.60m;
    private const string TeamLeadSessionIdFormat = "teamlead-{0}-{1:N}";

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
        var specialistExecutors = new List<ExecutorBinding>();
        var workflowInputExecutor = ExecutorBindingExtensions.BindAsExecutor<WorkflowRequest, WorkflowRequest>(
            (WorkflowRequest input, CancellationToken _) => ValueTask.FromResult(input),
            "teamlead-workflow-input",
            ExecutorOptions.Default,
            threadsafe: true);

        foreach (var role in roles)
        {
            var agent = _agentRegistry.Get(role);
            if (agent is null)
            {
                continue;
            }

            var executorId = $"agent-{role.ToString().ToLowerInvariant()}";
            var specialistExecutor = ExecutorBindingExtensions.BindAsExecutor<WorkflowRequest, AgentResponse>(
                (WorkflowRequest input, CancellationToken token) => new ValueTask<AgentResponse>(agent.RespondAsync(new AgentContext
                {
                    WorkflowRequest = input,
                    SharedNotes = sharedNotes
                }, token)),
                executorId,
                ExecutorOptions.Default,
                threadsafe: true);

            specialistExecutors.Add(specialistExecutor);
        }

        var specialistResponses = new List<AgentResponse>();
        if (specialistExecutors.Count > 0)
        {
            var builder = new WorkflowBuilder(workflowInputExecutor)
                .WithName("TeamLeadSpecialistWorkflow")
                .WithDescription("Routes the request through recommended specialist agents.")
                .WithOutputFrom(specialistExecutors.ToArray());

            foreach (var specialistExecutor in specialistExecutors)
            {
                builder.BindExecutor(specialistExecutor)
                    .AddEdge(workflowInputExecutor, specialistExecutor);
            }

            var workflow = builder.Build(validateOrphans: true);
            var sessionId = string.Format(TeamLeadSessionIdFormat, request.WorkflowType, Guid.NewGuid());
            await using var run = await InProcessExecution.RunAsync(
                workflow,
                request,
                sessionId: sessionId,
                cancellationToken: cancellationToken);

            specialistResponses = run.OutgoingEvents
                .OfType<WorkflowOutputEvent>()
                .Select(static output => output.As<AgentResponse>())
                .Where(static response => response is not null)
                .Cast<AgentResponse>()
                .ToList();
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
