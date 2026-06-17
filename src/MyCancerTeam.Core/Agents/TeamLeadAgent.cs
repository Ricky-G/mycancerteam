using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Agents.AI.Workflows;
using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Core.Agents;

public sealed class TeamLeadAgent : ITeamLeadAgent
{
    private const decimal MinimumConfidenceLevel = 0.20m;
    private const decimal StandardConfidenceLevel = 0.60m;
    private const decimal SynthesisedConfidenceLevel = 0.70m;
    private const string TeamLeadSessionIdTemplate = "teamlead-{0}-{1:N}";
    private const string TeamLeadWorkflowInputExecutorId = "teamlead-workflow-input";

    private const string SynthesisSystemPrompt = """
        You are the Team Lead facilitator of a multi-disciplinary oncology team (MDT). You receive structured viewpoints from several specialist AI agents on the same patient situation.

        Produce ONE unified MDT view that reads as a single coherent clinical opinion, not as a list of separate specialists. Integrate, reconcile, and de-duplicate the inputs. Where specialists disagree, surface the disagreement explicitly. Preserve every distinct fact; do not invent new facts; do not name the individual specialist agents.

        You may also receive the previously recorded MDT state (prior diagnosis, prior treatment, and prior open questions). Treat the new specialist viewpoints as the latest information arriving over time. As more information comes in you MUST resolve and remove previously open questions that are now answered: only carry forward open questions that remain genuinely unresolved, and add new ones only when the new information raises them. Do the same for clinician questions. The diagnosis and treatment should reflect the most current, complete picture, building on the prior state rather than discarding it.

        Protect patient privacy: never include the patient's name or other personal identifiers in your output. Refer to the patient only as "the patient". If a name appears in the inputs, replace it with "the patient" (or "[REDACTED]" for any other identifying detail such as contact information).

        Respond with a JSON object containing exactly these fields:
        {
          "currentDiagnosis": "<unified patient-friendly description of the current diagnostic picture, 2-5 sentences>",
          "currentTreatment": "<unified description of the current treatment status and plan, 2-5 sentences>",
          "openQuestions": ["<MDT-level open question 1>", "<MDT-level open question 2>"],
          "clinicianQuestions": ["<question to raise with the care team 1>", "<question to raise with the care team 2>", "<question to raise with the care team 3>"],
          "confidenceLevel": <number between 0.0 and 1.0 representing MDT confidence in this synthesis>
        }
        """;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IAgentRegistry _agentRegistry;
    private readonly WorkflowRouter _workflowRouter;
    private readonly ILlmChatClient? _llmClient;

    public TeamLeadAgent(IAgentRegistry agentRegistry, WorkflowRouter workflowRouter)
        : this(agentRegistry, workflowRouter, null)
    {
    }

    public TeamLeadAgent(IAgentRegistry agentRegistry, WorkflowRouter workflowRouter, ILlmChatClient? llmClient)
    {
        _agentRegistry = agentRegistry;
        _workflowRouter = workflowRouter;
        _llmClient = llmClient;
    }

    public AgentRole Role => AgentRole.TeamLead;
    public string Name => "Team Lead Agent";

    public bool CanHandle(AgentContext context) => true;

    public Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
        => CoordinateAsync(context.WorkflowRequest, context.SharedNotes, cancellationToken);

    public async Task<AgentResponse> CoordinateAsync(WorkflowRequest request, string sharedNotes, CancellationToken cancellationToken = default)
    {
        var roles = _workflowRouter.GetRecommendedRoles(request);
        var sessionId = string.Format(TeamLeadSessionIdTemplate, request.WorkflowType, Guid.NewGuid());
        var specialistExecutors = new List<ExecutorBinding>();
        var engagedAgentNames = new List<string>();
        var workflowInputExecutor = ExecutorBindingExtensions.BindAsExecutor<WorkflowRequest, WorkflowRequest>(
            (WorkflowRequest input, CancellationToken _) => ValueTask.FromResult(input),
            TeamLeadWorkflowInputExecutorId,
            ExecutorOptions.Default,
            threadsafe: true);

        foreach (var role in roles)
        {
            var agent = _agentRegistry.Get(role);
            if (agent is null)
            {
                continue;
            }

            engagedAgentNames.Add(agent.Name);

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

        var engagedAgents = engagedAgentNames
            .Distinct()
            .ToList();

        var synthesis = await SynthesizeAsync(request, specialistResponses, sharedNotes, cancellationToken);
        if (synthesis is not null)
        {
            return new AgentResponse
            {
                Role = Role,
                Summary = synthesis.CurrentDiagnosis ?? string.Empty,
                TechnicalSummary = synthesis.CurrentTreatment ?? string.Empty,
                ConfidenceLevel = synthesis.ConfidenceLevel,
                OpenQuestions = synthesis.OpenQuestions is null || synthesis.OpenQuestions.Count == 0 ? openQuestions : synthesis.OpenQuestions,
                SuggestedClinicianQuestions = synthesis.ClinicianQuestions is null || synthesis.ClinicianQuestions.Count == 0 ? suggestions : synthesis.ClinicianQuestions,
                EngagedAgents = engagedAgents,
                Citations = citations
            };
        }

        var summary = BuildAggregatedText(specialistResponses, r => r.Summary, "Awaiting specialist input.");
        var technicalSummary = BuildAggregatedText(specialistResponses, r => r.TechnicalSummary, "No specialist treatment notes yet.");

        return new AgentResponse
        {
            Role = Role,
            Summary = summary,
            TechnicalSummary = technicalSummary,
            ConfidenceLevel = specialistResponses.Count == 0 ? MinimumConfidenceLevel : StandardConfidenceLevel,
            OpenQuestions = openQuestions,
            SuggestedClinicianQuestions = suggestions,
            EngagedAgents = engagedAgents,
            Citations = citations
        };
    }

    private async Task<SynthesisDto?> SynthesizeAsync(WorkflowRequest request, IReadOnlyList<AgentResponse> specialistResponses, string sharedNotes, CancellationToken cancellationToken)
    {
        if (_llmClient is null || specialistResponses.Count == 0)
        {
            return null;
        }

        try
        {
            var userMessage = BuildSynthesisUserMessage(request, specialistResponses, sharedNotes);
            var json = await _llmClient.CompleteAsync(SynthesisSystemPrompt, userMessage, cancellationToken);
            var parsed = JsonSerializer.Deserialize<SynthesisDto>(json, JsonOptions);

            if (parsed is null
                || string.IsNullOrWhiteSpace(parsed.CurrentDiagnosis)
                || string.IsNullOrWhiteSpace(parsed.CurrentTreatment))
            {
                return null;
            }

            parsed.OpenQuestions ??= [];
            parsed.ClinicianQuestions ??= [];
            if (parsed.ConfidenceLevel <= 0m)
            {
                parsed.ConfidenceLevel = SynthesisedConfidenceLevel;
            }
            else if (parsed.ConfidenceLevel > 1m)
            {
                parsed.ConfidenceLevel = 1m;
            }

            return parsed;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return null;
        }
    }

    private static string BuildSynthesisUserMessage(WorkflowRequest request, IReadOnlyList<AgentResponse> specialistResponses, string sharedNotes)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Workflow type: {request.WorkflowType}");
        builder.AppendLine($"Patient input: {request.UserInput}");
        builder.AppendLine();

        var prior = ExtractPriorState(sharedNotes);
        if (prior is not null)
        {
            builder.AppendLine("Previously recorded MDT state (build on this; resolve and drop questions now answered by the new viewpoints below):");
            if (!string.IsNullOrWhiteSpace(prior.Summary))
            {
                builder.AppendLine($"- Prior diagnosis/summary: {prior.Summary.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(prior.Treatment))
            {
                builder.AppendLine($"- Prior treatment: {prior.Treatment.Trim()}");
            }

            if (prior.OpenQuestions.Count > 0)
            {
                builder.AppendLine("- Previously open questions / next steps (keep only those still unresolved):");
                foreach (var question in prior.OpenQuestions)
                {
                    builder.AppendLine($"  * {question}");
                }
            }

            builder.AppendLine();
        }

        builder.AppendLine("Specialist viewpoints (do NOT name these specialists in your output; integrate them into one MDT voice):");

        for (var i = 0; i < specialistResponses.Count; i++)
        {
            var response = specialistResponses[i];
            builder.AppendLine($"## Viewpoint {i + 1} ({response.Role})");
            if (!string.IsNullOrWhiteSpace(response.Summary))
            {
                builder.AppendLine($"- Patient-friendly: {response.Summary.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(response.TechnicalSummary))
            {
                builder.AppendLine($"- Clinical: {response.TechnicalSummary.Trim()}");
            }

            if (response.OpenQuestions.Count > 0)
            {
                builder.AppendLine("- Open questions:");
                foreach (var question in response.OpenQuestions)
                {
                    builder.AppendLine($"  * {question}");
                }
            }

            if (response.SuggestedClinicianQuestions.Count > 0)
            {
                builder.AppendLine("- Suggested clinician questions:");
                foreach (var question in response.SuggestedClinicianQuestions)
                {
                    builder.AppendLine($"  * {question}");
                }
            }

            builder.AppendLine();
        }

        return builder.ToString();
    }

    private static string BuildAggregatedText(IReadOnlyList<AgentResponse> responses, Func<AgentResponse, string> selector, string emptyMessage)
    {
        if (responses.Count == 0)
        {
            return emptyMessage;
        }

        var builder = new StringBuilder();
        foreach (var response in responses)
        {
            var text = selector(response)?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                continue;
            }

            builder.AppendLine($"- {response.Role}: {text}");
        }

        var aggregated = builder.ToString().Trim();
        return string.IsNullOrEmpty(aggregated) ? emptyMessage : aggregated;
    }

    private static PriorState? ExtractPriorState(string previousSummary)
    {
        if (string.IsNullOrWhiteSpace(previousSummary))
        {
            return null;
        }

        // The previous summary is the rendered summary.md document with level-2 sections.
        var summary = ExtractSection(previousSummary, "Current Diagnosis") ?? string.Empty;
        var treatment = ExtractSection(previousSummary, "Current Treatment") ?? string.Empty;
        var openQuestions = ExtractSectionLines(previousSummary, "Next Steps");

        if (string.IsNullOrWhiteSpace(summary)
            && string.IsNullOrWhiteSpace(treatment)
            && openQuestions.Count == 0)
        {
            return null;
        }

        return new PriorState(summary, treatment, openQuestions);
    }

    private static string? ExtractSection(string document, string heading)
    {
        var lines = ExtractSectionLines(document, heading);
        return lines.Count == 0 ? null : string.Join(Environment.NewLine, lines);
    }

    private static IReadOnlyList<string> ExtractSectionLines(string document, string heading)
    {
        var match = Regex.Match(document, $@"(?ms)^#{{2,3}} {Regex.Escape(heading)}\s*(?<content>.*?)(?=^#{{2,3}} |\z)");
        if (!match.Success)
        {
            return [];
        }

        return match.Groups["content"].Value
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(line => line.StartsWith("- ") ? line[2..].Trim() : line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("_Last updated:", StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    private sealed record PriorState(string Summary, string Treatment, IReadOnlyList<string> OpenQuestions);

    private sealed class SynthesisDto
    {
        public string? CurrentDiagnosis { get; set; }
        public string? CurrentTreatment { get; set; }
        public List<string>? OpenQuestions { get; set; }
        public List<string>? ClinicianQuestions { get; set; }
        public decimal ConfidenceLevel { get; set; }
    }
}
