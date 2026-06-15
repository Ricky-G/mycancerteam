using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Models;
using MyCancerTeam.Core.Research;
using MyCancerTeam.Infrastructure.Research;

namespace MyCancerTeam.Tests;

public sealed class ResearchOncologyServiceTests
{
    [Fact]
    public async Task GetLatestEvidenceAsync_ShouldUseLiveEvidenceAndReturnCitations()
    {
        var evidence = new ResearchEvidenceSnapshot
        {
            Sources =
            [
                new ResearchEvidenceSource
                {
                    Citation = new CitationMetadata
                    {
                        SourceName = "PubMed",
                        Title = "Current Oncology Evidence",
                        Url = "https://pubmed.ncbi.nlm.nih.gov/12345678/",
                        EvidenceLevel = "publication",
                        PublishedOn = new DateOnly(2026, 6, 1)
                    },
                    Summary = "Recent article on current oncology evidence."
                }
            ],
            Warnings = ["ClinicalTrials.gov returned no studies."]
        };

        var llm = new CapturingLlmChatClient(
            """
            {
              "patientFriendlySummary": "Current evidence looks promising.",
              "technicalSummary": "Recent publication supports the approach.",
              "suggestedQuestions": ["What is the evidence level?"],
              "evidenceGapNotes": ["More data may be needed."]
            }
            """);

        var service = new ResearchOncologyService(llm, new StubEvidenceProvider(evidence));

        var update = await service.GetLatestEvidenceAsync("Stage II breast cancer, HER2 positive");

        Assert.Contains("Live web evidence:", llm.UserMessage);
        Assert.Contains("Current Oncology Evidence", llm.UserMessage);
        Assert.Contains("https://pubmed.ncbi.nlm.nih.gov/12345678/", llm.UserMessage);
        Assert.Single(update.Citations);
        Assert.Equal("Current Oncology Evidence", update.Citations[0].Title);
        Assert.Contains("ClinicalTrials.gov returned no studies.", update.EvidenceGapNotes);
    }

    [Fact]
    public async Task GetLatestEvidenceAsync_WhenNoEvidence_FallsBackToTrainingKnowledge()
    {
        var llm = new CapturingLlmChatClient(
            """
            {
              "patientFriendlySummary": "Based on general training knowledge, here are typical options.",
              "technicalSummary": "Per NCCN-style guidelines (training knowledge, verify current sources).",
              "suggestedQuestions": ["What is the current standard of care?"],
              "evidenceGapNotes": []
            }
            """);

        var service = new ResearchOncologyService(llm, new StubEvidenceProvider(new ResearchEvidenceSnapshot
        {
            Warnings = ["PubMed unreachable."]
        }));

        var update = await service.GetLatestEvidenceAsync("Stage IV NSCLC, EGFR mutation");

        Assert.Contains("NONE AVAILABLE", llm.UserMessage);
        Assert.Contains("training knowledge", llm.SystemPrompt!, StringComparison.OrdinalIgnoreCase);
        Assert.Contains(update.EvidenceGapNotes, note => note.Contains("training data", StringComparison.OrdinalIgnoreCase));
        Assert.Contains("PubMed unreachable.", update.EvidenceGapNotes);
        Assert.Empty(update.Citations);
    }

    private sealed class CapturingLlmChatClient : ILlmChatClient
    {
        private readonly string _response;

        public CapturingLlmChatClient(string response)
        {
            _response = response;
        }

        public string? SystemPrompt { get; private set; }
        public string? UserMessage { get; private set; }

        public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            SystemPrompt = systemPrompt;
            UserMessage = userMessage;
            return Task.FromResult(_response);
        }

        public Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            SystemPrompt = systemPrompt;
            UserMessage = userMessage;
            return Task.FromResult(_response);
        }
    }

    private sealed class StubEvidenceProvider : IResearchEvidenceProvider
    {
        private readonly ResearchEvidenceSnapshot _snapshot;

        public StubEvidenceProvider(ResearchEvidenceSnapshot snapshot)
        {
            _snapshot = snapshot;
        }

        public Task<ResearchEvidenceSnapshot> GetEvidenceAsync(string patientContext, CancellationToken cancellationToken = default)
            => Task.FromResult(_snapshot);
    }
}
