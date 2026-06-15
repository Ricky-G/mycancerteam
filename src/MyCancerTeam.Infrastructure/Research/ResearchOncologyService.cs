using System.Text;
using System.Text.Json;
using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Models;
using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public sealed class ResearchOncologyService : IResearchOncologyService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string SystemPromptWithEvidence = """
        You are an oncology research synthesis AI agent with access to live web evidence retrieved for this request.

        Use the patient context and the supplied live evidence to synthesize current treatment options, clinical trials, and relevant guidelines.
        Do not invent sources or cite anything not present in the evidence block.
        If the evidence is sparse, conflicting, or outdated, say so explicitly.

        Respond with a JSON object containing exactly these fields:
        {
          "patientFriendlySummary": "<accessible summary of relevant evidence and options, 2–4 sentences>",
          "technicalSummary": "<detailed clinical research summary referencing guidelines, trial classes, or evidence levels where known>",
          "suggestedQuestions": ["<research-informed question 1>", "<research-informed question 2>", "<research-informed question 3>"],
          "evidenceGapNotes": ["<gap or limitation 1>", "<gap or limitation 2>"]
        }
        """;

    private const string SystemPromptOfflineFallback = """
        You are an oncology research synthesis AI agent. Live web retrieval failed for this request, so no fresh sources are available right now.

        Fall back on your training knowledge to give a useful, careful synthesis of widely-recognised treatment options, established clinical-trial classes, and major guideline bodies (e.g. NCCN, ESMO, ASCO).
        - Be explicit that this is from your training data, not current literature, and may be out of date.
        - Do not fabricate specific citations, URLs, or trial IDs.
        - Recommend the team verify against current sources (PubMed, ClinicalTrials.gov, society guidelines).
        - Flag uncertainty whenever you are unsure.

        Respond with a JSON object containing exactly these fields:
        {
          "patientFriendlySummary": "<accessible summary, 2–4 sentences, noting the offline caveat>",
          "technicalSummary": "<detailed clinical research summary based on training knowledge, with explicit out-of-date / verify caveat>",
          "suggestedQuestions": ["<research-informed question 1>", "<research-informed question 2>", "<research-informed question 3>"],
          "evidenceGapNotes": ["Live web evidence was unavailable; synthesis is from model training data and may be outdated."]
        }
        """;

    private readonly ILlmChatClient _llmClient;
    private readonly IResearchEvidenceProvider? _evidenceProvider;

    public ResearchOncologyService(ILlmChatClient llmClient)
        : this(llmClient, null)
    {
    }

    public ResearchOncologyService(ILlmChatClient llmClient, IResearchEvidenceProvider? evidenceProvider)
    {
        _llmClient = llmClient;
        _evidenceProvider = evidenceProvider;
    }

    public async Task<ResearchUpdate> GetLatestEvidenceAsync(string patientContext, CancellationToken cancellationToken = default)
    {
        var evidence = _evidenceProvider is null
            ? new ResearchEvidenceSnapshot()
            : await _evidenceProvider.GetEvidenceAsync(patientContext, cancellationToken);

        var hasEvidence = evidence.Sources.Count > 0;
        var systemPrompt = hasEvidence ? SystemPromptWithEvidence : SystemPromptOfflineFallback;
        var userMessage = BuildUserMessage(patientContext, evidence, hasEvidence);
        var json = await _llmClient.CompleteAsync(systemPrompt, userMessage, cancellationToken);

        var citations = evidence.Sources.Select(source => source.Citation).ToArray();
        var parsed = ParseResponse(json);
        if (parsed is null)
        {
            return FallbackUpdate(evidence.Warnings, citations);
        }

        var evidenceGapNotes = new List<string>(parsed.EvidenceGapNotes);
        evidenceGapNotes.AddRange(evidence.Warnings);
        if (!hasEvidence && !evidenceGapNotes.Any(note => note.Contains("training", StringComparison.OrdinalIgnoreCase)))
        {
            evidenceGapNotes.Insert(0, "Live web evidence unavailable; synthesis is from model training data and may be outdated.");
        }

        return new ResearchUpdate
        {
            PatientFriendlySummary = parsed.PatientFriendlySummary,
            TechnicalSummary = parsed.TechnicalSummary,
            SuggestedQuestions = parsed.SuggestedQuestions,
            EvidenceGapNotes = evidenceGapNotes,
            Citations = citations
        };
    }

    private static string BuildUserMessage(string patientContext, ResearchEvidenceSnapshot evidence, bool hasEvidence)
    {
        var builder = new StringBuilder();
        builder.AppendLine("Patient context:");
        builder.AppendLine(string.IsNullOrWhiteSpace(patientContext) ? "(not provided)" : patientContext.Trim());
        builder.AppendLine();

        if (hasEvidence)
        {
            builder.AppendLine("Live web evidence:");
            for (var i = 0; i < evidence.Sources.Count; i++)
            {
                var source = evidence.Sources[i];
                builder.AppendLine($"- Source {i + 1}: {source.Citation.SourceName}");
                builder.AppendLine($"  Title: {source.Citation.Title}");
                builder.AppendLine($"  URL: {source.Citation.Url}");
                if (source.Citation.PublishedOn is not null)
                {
                    builder.AppendLine($"  Published: {source.Citation.PublishedOn:yyyy-MM-dd}");
                }

                if (!string.IsNullOrWhiteSpace(source.Citation.EvidenceLevel))
                {
                    builder.AppendLine($"  Evidence level: {source.Citation.EvidenceLevel}");
                }

                if (!string.IsNullOrWhiteSpace(source.Summary))
                {
                    builder.AppendLine($"  Summary: {source.Summary}");
                }
            }
        }
        else
        {
            builder.AppendLine("Live web evidence: NONE AVAILABLE this run. Fall back on your training knowledge as instructed.");
        }

        if (evidence.Warnings.Count > 0)
        {
            builder.AppendLine();
            builder.AppendLine("Retrieval notes:");
            foreach (var warning in evidence.Warnings)
            {
                builder.AppendLine($"- {warning}");
            }
        }

        builder.AppendLine();
        builder.AppendLine(hasEvidence
            ? "Write your answer using the live evidence above."
            : "Write your answer from training knowledge, with explicit out-of-date caveat and a recommendation to verify against current sources.");
        return builder.ToString();
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

    private static ResearchUpdate FallbackUpdate(IReadOnlyList<string> warnings, IReadOnlyList<CitationMetadata> citations) => new()
    {
        PatientFriendlySummary = "Research synthesis could not be completed. Please retry or consult your oncology team for the latest evidence.",
        TechnicalSummary = string.Empty,
        SuggestedQuestions = ["Please retry the research query or consult your clinical team."],
        EvidenceGapNotes = ["Research response could not be parsed.", ..warnings],
        Citations = citations
    };

    private sealed class ResearchResponseDto
    {
        public string? PatientFriendlySummary { get; set; }
        public string? TechnicalSummary { get; set; }
        public List<string>? SuggestedQuestions { get; set; }
        public List<string>? EvidenceGapNotes { get; set; }
    }
}
