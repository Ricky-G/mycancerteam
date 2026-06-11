using System.Text.Json;
using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public sealed class ResearchOncologyService : IResearchOncologyService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string SystemPrompt = """
        You are an oncology research synthesis AI agent. Based on the patient context provided, synthesise a research evidence summary that supports the care team in understanding current treatment options, clinical trials, and relevant guidelines.

        Important: You do not have access to live databases. Summarise based on your training knowledge and clearly flag knowledge cut-off limitations.

        Respond with a JSON object containing exactly these fields:
        {
          "patientFriendlySummary": "<accessible summary of relevant evidence and options, 2–4 sentences>",
          "technicalSummary": "<detailed clinical research summary referencing guidelines, trial classes, or evidence levels where known>",
          "suggestedQuestions": ["<research-informed question 1>", "<research-informed question 2>", "<research-informed question 3>"],
          "evidenceGapNotes": ["<gap or limitation 1>", "<gap or limitation 2>"]
        }
        """;

    private readonly ILlmChatClient _llmClient;

    public ResearchOncologyService(ILlmChatClient llmClient)
    {
        _llmClient = llmClient;
    }

    public async Task<ResearchUpdate> GetLatestEvidenceAsync(string patientContext, CancellationToken cancellationToken = default)
    {
        // TODO: Integrate PubMed, guideline APIs, ClinicalTrials.gov, and curated oncology journal feeds.
        // TODO: Add patient-specific filtering by subtype, biomarkers, line of treatment, and geography.
        // TODO: Add evidence grading and conflict-resolution logic across sources.
        var json = await _llmClient.CompleteAsync(SystemPrompt, patientContext, cancellationToken);
        return ParseResponse(json) ?? FallbackUpdate();
    }

    private static ResearchUpdate? ParseResponse(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<ResearchResponseDto>(json, JsonOptions);
            if (dto is null)
            {
                return null;
            }

            return new ResearchUpdate
            {
                PatientFriendlySummary = dto.PatientFriendlySummary ?? string.Empty,
                TechnicalSummary = dto.TechnicalSummary ?? string.Empty,
                SuggestedQuestions = dto.SuggestedQuestions ?? [],
                EvidenceGapNotes = dto.EvidenceGapNotes ?? []
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ResearchUpdate FallbackUpdate() => new()
    {
        PatientFriendlySummary = "Research synthesis could not be completed. Please retry or consult your oncology team for the latest evidence.",
        TechnicalSummary = string.Empty,
        SuggestedQuestions = ["Please retry the research query or consult your clinical team."],
        EvidenceGapNotes = ["Research response could not be parsed."]
    };

    private sealed class ResearchResponseDto
    {
        public string? PatientFriendlySummary { get; set; }
        public string? TechnicalSummary { get; set; }
        public List<string>? SuggestedQuestions { get; set; }
        public List<string>? EvidenceGapNotes { get; set; }
    }
}
