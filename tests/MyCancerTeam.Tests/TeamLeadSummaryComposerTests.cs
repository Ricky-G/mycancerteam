using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Infrastructure.Notes;

namespace MyCancerTeam.Tests;

public sealed class TeamLeadSummaryComposerTests
{
    [Fact]
    public void Compose_RendersLatestResponseAsCurrentState()
    {
        var summary = TeamLeadSummaryComposer.Compose(PreviousSummary(), "Interactive input", "Need an update", SampleResponse());

        AssertContainsCoreContent(summary);
    }

    [Fact]
    public async Task ComposeAsync_WithoutLlm_RendersLatestResponse()
    {
        var composer = new TeamLeadSummaryComposer();

        var summary = await composer.ComposeAsync(PreviousSummary(), "Interactive input", "Need an update", SampleResponse());

        AssertContainsCoreContent(summary);
    }

    [Fact]
    public async Task ComposeAsync_FallsBackToPreviousSummary_WhenResponseSectionEmpty()
    {
        var composer = new TeamLeadSummaryComposer();

        // Response carries no treatment, so the prior treatment must be preserved (never regress
        // an already-known fact).
        var response = new AgentResponse
        {
            Role = AgentRole.TeamLead,
            Summary = "Stage III disease confirmed on latest review.",
            TechnicalSummary = string.Empty,
            ConfidenceLevel = 0.8m,
            SuggestedClinicianQuestions = ["Confirm the next imaging date?"],
            EngagedAgents = ["Research Oncology Agent"]
        };

        var summary = await composer.ComposeAsync(PreviousSummary(), "Interactive input", "Need an update", response);

        Assert.Contains("Stage III disease confirmed on latest review.", summary);
        Assert.Contains("Adjuvant chemotherapy underway.", summary);
        Assert.Contains("Confirm the next imaging date?", summary);
        // Engaged agents are a union of the latest response and the carried-forward summary.
        Assert.Contains("Research Oncology Agent", summary);
        Assert.Contains("Patient Owner Agent", summary);
    }

    [Fact]
    public async Task ComposeAsync_ExcludingDiagnosisContribution_PreservesPriorDiagnosis()
    {
        var composer = new TeamLeadSummaryComposer();
        var response = new AgentResponse
        {
            Role = AgentRole.TeamLead,
            Summary = "Insurance note mentions prior staging details.",
            TechnicalSummary = "Prior authorization submitted.",
            ConfidenceLevel = 0.8m,
            SuggestedClinicianQuestions = ["Confirm insurer timeline?"],
            EngagedAgents = ["Patient Owner Agent"]
        };

        var summary = await composer.ComposeAsync(
            PreviousSummary(),
            "File: C:\\repo\\.local\\non-medical-notes\\insurance.txt",
            "Insurance update",
            response,
            includeDiagnosisContribution: false);

        Assert.Contains("Stage II disease being reviewed.", summary);
        Assert.DoesNotContain("Insurance note mentions prior staging details.", summary);
        Assert.Contains("Prior authorization submitted.", summary);
    }

    [Fact]
    public async Task ComposeAsync_WithLlm_UsesLlmFormattedOutput()
    {
        var llm = new RecordingChatClient("""
            # MyCancerTeam Summary

            ## Current Diagnosis
            - LLM polished diagnosis line.

            ## Current Treatment
            - LLM polished treatment line.

            ## Next Steps
            - LLM polished next step.

            ## Engaged Agents
            Patient Owner Agent, Research Oncology Agent
            """);
        var composer = new TeamLeadSummaryComposer(llm);

        var summary = await composer.ComposeAsync(PreviousSummary(), "Interactive input", "Need an update", SampleResponse());

        Assert.Contains("LLM polished diagnosis line.", summary);
        Assert.Contains("LLM polished treatment line.", summary);
        Assert.Contains("_Last updated:", summary);
        // The deterministic rendering fed to the LLM is built from the latest response.
        Assert.Contains("Stage II disease confirmed on latest review.", llm.LastUserMessage);
    }

    [Fact]
    public async Task ComposeAsync_WhenLlmFails_FallsBackToDeterministicSummary()
    {
        var composer = new TeamLeadSummaryComposer(new ThrowingChatClient());

        var summary = await composer.ComposeAsync(PreviousSummary(), "Interactive input", "Need an update", SampleResponse());

        AssertContainsCoreContent(summary);
    }

    private static AgentResponse SampleResponse() => new()
    {
        Role = AgentRole.TeamLead,
        Summary = "Stage II disease confirmed on latest review.",
        TechnicalSummary = "Adjuvant therapy plan finalized after pathology.",
        ConfidenceLevel = 0.85m,
        OpenQuestions = ["What is the follow-up schedule?"],
        SuggestedClinicianQuestions = ["Review radiation options"],
        EngagedAgents = ["Patient Owner Agent", "Research Oncology Agent"]
    };

    private static string PreviousSummary() =>
        """
        # MyCancerTeam Summary

        _Last updated: 2026-01-01T00:00:00Z_

        ## Current Diagnosis
        Stage II disease being reviewed.

        ## Current Treatment
        Adjuvant chemotherapy underway.

        ## Next Steps
        - Confirm pathology details
        - Review adjuvant therapy options

        ## Engaged Agents
        Patient Owner Agent
        """;

    private static void AssertContainsCoreContent(string summary)
    {
        Assert.Contains("## Current Diagnosis", summary);
        Assert.Contains("## Current Treatment", summary);
        Assert.Contains("## Next Steps", summary);
        Assert.Contains("## Engaged Agents", summary);
        Assert.Contains("Stage II disease confirmed on latest review.", summary);
        Assert.Contains("Adjuvant therapy plan finalized after pathology.", summary);
        Assert.Contains("Review radiation options", summary);
        Assert.Contains("Patient Owner Agent", summary);
        Assert.Contains("Research Oncology Agent", summary);
        Assert.DoesNotContain("Need an update", summary);
    }

    private sealed class RecordingChatClient : ILlmChatClient
    {
        private readonly string _textResponse;

        public RecordingChatClient(string textResponse)
        {
            _textResponse = textResponse;
        }

        public string? LastUserMessage { get; private set; }

        public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();

        public Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(_textResponse);
        }
    }

    private sealed class ThrowingChatClient : ILlmChatClient
    {
        public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");

        public Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("boom");
    }
}
