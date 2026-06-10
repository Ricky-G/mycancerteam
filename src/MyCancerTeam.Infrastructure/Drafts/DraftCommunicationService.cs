using MyCancerTeam.Core.Drafts;

namespace MyCancerTeam.Infrastructure.Drafts;

public sealed class DraftCommunicationService : IDraftCommunicationService
{
    private readonly IMarkdownDraftExporter _exporter;

    public DraftCommunicationService(IMarkdownDraftExporter exporter)
    {
        _exporter = exporter;
    }

    public async Task<DraftCommunicationResult> CreateDraftAsync(DraftCommunicationRequest request, CancellationToken cancellationToken = default)
    {
        var markdown = $"""
# Draft: {request.Subject}

- Timestamp (UTC): {DateTimeOffset.UtcNow:O}
- Draft type: {request.DraftType}
- Recipient: {request.RecipientType}
- Tone: Calm, respectful, and clear

## Context Summary
{request.PatientContextSummary}

## Message (Concise Version)
Hello [Recipient Name],

I am writing regarding [patient name / reference]. I would appreciate your guidance on **{request.Subject}**.

Could you please advise on the next steps and any required documents?

Thank you for your time and support.

Kind regards,
[Your Name]

## Message (Detailed Version)
Hello [Recipient Name],

I am writing to request support regarding **{request.Subject}**.

### Relevant Context
- Diagnosis/context: [add]
- Current treatment status: [add]
- Key constraints: [add]
- Requested timeline: [add]

### Specific Questions
1. [question 1]
2. [question 2]
3. [question 3]

### Additional Details
{request.AdditionalDetails}

Please let me know if any details are missing, and I will provide them promptly.

Kind regards,
[Your Name]

## Safety and Validation Note
Please verify all major medical decisions directly with qualified healthcare professionals.
""";

        var path = await _exporter.ExportAsync(request.DraftType, markdown, cancellationToken);

        return new DraftCommunicationResult
        {
            FilePath = path,
            MarkdownContent = markdown
        };
    }
}
