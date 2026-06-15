using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Infrastructure.Notes;

namespace MyCancerTeam.Tests;

public sealed class TeamLeadSummaryComposerTests
{
    [Fact]
    public void Compose_ShouldUseCumulativeNotesLog()
    {
        var response = SampleResponse();

        var summary = TeamLeadSummaryComposer.Compose(SampleNotes(), "Interactive input", "Need an update", response);

        AssertContainsCoreContent(summary);
    }

    [Fact]
    public async Task ComposeAsync_WithoutLlm_ReturnsDeterministicSummary()
    {
        var composer = new TeamLeadSummaryComposer();

        var summary = await composer.ComposeAsync(SampleNotes(), "Interactive input", "Need an update", SampleResponse());

        AssertContainsCoreContent(summary);
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

        var summary = await composer.ComposeAsync(SampleNotes(), "Interactive input", "Need an update", SampleResponse());

        Assert.Contains("LLM polished diagnosis line.", summary);
        Assert.Contains("LLM polished treatment line.", summary);
        Assert.Contains("_Last updated:", summary);
        Assert.Contains("Stage II disease being reviewed.", llm.LastUserMessage);
    }

    [Fact]
    public async Task ComposeAsync_WhenLlmFails_FallsBackToDeterministicSummary()
    {
        var composer = new TeamLeadSummaryComposer(new ThrowingChatClient());

        var summary = await composer.ComposeAsync(SampleNotes(), "Interactive input", "Need an update", SampleResponse());

        AssertContainsCoreContent(summary);
    }

    [Fact]
    public void Compose_ShouldPreferLatestUnifiedUpdateOverEarlierRoleLabeledUpdate()
    {
        // Reproduces the bug where the composer picked the OLDEST update (legacy "- Role: ..."
        // aggregation) instead of the newest unified MDT synthesis.
        var notes =
            """
            ## Update 1
            Source: Interactive input
            User input: legacy

            ### Current Diagnosis
            - PatientOwner: Legacy per-role diagnosis line that should not win.
            - Radiologist: Another legacy per-role line.

            ### Current Treatment
            - MedicalOncologist: Legacy per-role treatment line.

            ### Next Steps
            - Legacy next step

            ### Engaged Agents
            - Patient Owner Agent

            ## Update 2
            Source: Interactive input
            User input: latest

            ### Current Diagnosis
            Stage III unified MDT diagnosis with no role labels.

            ### Current Treatment
            Unified MDT treatment plan with no role labels.

            ### Next Steps
            - Latest unified next step

            ### Engaged Agents
            - Research Oncology Agent
            """;

        var summary = TeamLeadSummaryComposer.Compose(notes, "Interactive input", "x", SampleResponse());

        Assert.Contains("Stage III unified MDT diagnosis with no role labels.", summary);
        Assert.Contains("Unified MDT treatment plan with no role labels.", summary);
        Assert.Contains("Latest unified next step", summary);
        Assert.DoesNotContain("PatientOwner:", summary);
        Assert.DoesNotContain("Radiologist:", summary);
        Assert.DoesNotContain("MedicalOncologist:", summary);
        Assert.DoesNotContain("Legacy", summary);
    }

    private static AgentResponse SampleResponse() => new()
    {
        Role = AgentRole.TeamLead,
        Summary = "This response should not drive the snapshot.",
        TechnicalSummary = "This response treatment should not appear either.",
        ConfidenceLevel = 0.85m,
        OpenQuestions = ["What is next?"],
        SuggestedClinicianQuestions = ["Should we confirm the plan?"],
        EngagedAgents = ["Patient Owner Agent", "Research Oncology Agent"]
    };

    private static string SampleNotes() =>
        """
        ## Update 1
        Source: Interactive input
        User input: first

        ### Current Diagnosis
        Stage II disease being reviewed.

        ### Current Treatment
        Surgery completed; adjuvant therapy pending.

        ### Next Steps
        - Confirm pathology details
        - Review adjuvant therapy options

        ### Engaged Agents
        - Patient Owner Agent

        ## Update 2
        Source: Interactive input
        User input: second

        ### Current Diagnosis
        Team synthesis prepared.

        ### Current Treatment
        Consolidated specialist viewpoints with tracked uncertainties and unresolved issues.

        ### Next Steps
        - Review radiation options
        - Discuss follow-up schedule

        ### Engaged Agents
        - Research Oncology Agent
        """;

    private static void AssertContainsCoreContent(string summary)
    {
        Assert.Contains("## Current Diagnosis", summary);
        Assert.Contains("## Current Treatment", summary);
        Assert.Contains("## Next Steps", summary);
        Assert.Contains("## Engaged Agents", summary);
        Assert.Contains("Stage II disease being reviewed.", summary);
        Assert.Contains("Surgery completed; adjuvant therapy pending.", summary);
        Assert.Contains("Review radiation options", summary);
        Assert.Contains("Patient Owner Agent", summary);
        Assert.Contains("Research Oncology Agent", summary);
        Assert.DoesNotContain("This response should not drive the snapshot.", summary);
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
