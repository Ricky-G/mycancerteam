using System.Text.Json;
using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Drafts;

namespace MyCancerTeam.Infrastructure.Drafts;

public sealed class DraftCommunicationService : IDraftCommunicationService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private const string SystemPrompt = """
        You are an expert communication drafting agent helping cancer patients and their support people communicate effectively with healthcare providers, insurers, trial coordinators, and other relevant parties.

        Write professional, empathetic, and clear communication that respects the patient's situation. Always include a validation note at the end reminding the reader to verify major decisions with qualified healthcare professionals.

        Respond with a JSON object containing exactly these fields:
        {
          "subject": "<refined or confirmed subject line>",
          "draftContent": "<full draft in markdown format, including a concise version and a detailed version with placeholders for patient-specific details>"
        }
        """;

    private readonly IMarkdownDraftExporter _exporter;
    private readonly ILlmChatClient _llmClient;

    public DraftCommunicationService(IMarkdownDraftExporter exporter, ILlmChatClient llmClient)
    {
        _exporter = exporter;
        _llmClient = llmClient;
    }

    public async Task<DraftCommunicationResult> CreateDraftAsync(DraftCommunicationRequest request, CancellationToken cancellationToken = default)
    {
        var userMessage = $"""
            Draft type: {request.DraftType}
            Recipient type: {request.RecipientType}
            Subject: {request.Subject}
            Patient context: {request.PatientContextSummary}
            Additional details: {request.AdditionalDetails}
            """;

        var json = await _llmClient.CompleteAsync(SystemPrompt, userMessage, cancellationToken);
        var (subject, draftContent) = ParseDraftResponse(json, request);

        var markdown = $"""
            # Draft: {subject}

            - Timestamp (UTC): {DateTimeOffset.UtcNow:O}
            - Draft type: {request.DraftType}
            - Recipient: {request.RecipientType}
            - Tone: Calm, respectful, and clear

            {draftContent}
            """;

        var path = await _exporter.ExportAsync(request.DraftType, markdown, cancellationToken);

        return new DraftCommunicationResult
        {
            FilePath = path,
            MarkdownContent = markdown
        };
    }

    private static (string subject, string draftContent) ParseDraftResponse(string json, DraftCommunicationRequest request)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<DraftResponseDto>(json, JsonOptions);
            if (dto is not null)
            {
                var subject = string.IsNullOrWhiteSpace(dto.Subject) ? request.Subject : dto.Subject;
                var content = string.IsNullOrWhiteSpace(dto.DraftContent)
                    ? "Draft content could not be generated. Please retry."
                    : dto.DraftContent;
                return (subject, content);
            }
        }
        catch (JsonException)
        {
        }

        return (request.Subject, "Draft content could not be generated. Please retry.");
    }

    private sealed class DraftResponseDto
    {
        public string? Subject { get; set; }
        public string? DraftContent { get; set; }
    }
}
