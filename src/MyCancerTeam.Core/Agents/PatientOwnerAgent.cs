using System.Text.Json;
using MyCancerTeam.Core.AI;

namespace MyCancerTeam.Core.Agents;

public sealed class PatientOwnerAgent : ISpecialistAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ILlmChatClient _llmClient;

    public PatientOwnerAgent(ILlmChatClient llmClient)
    {
        _llmClient = llmClient;
    }

    public AgentRole Role => AgentRole.PatientOwner;
    public string Name => "Patient / Support Person Owner Agent";

    public bool CanHandle(AgentContext context) => true;

    public async Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        const string systemPrompt = """
            You are an AI patient advocate agent helping a cancer patient and their support person capture their priorities, concerns, and practical realities for the care team.

            Focus on: patient preferences, daily-function concerns, caregiver constraints, unresolved worries, and quality-of-life considerations.

            Respond with a JSON object containing exactly these fields:
            {
              "summary": "<patient-friendly summary capturing priorities and practical realities, 2–4 sentences>",
              "technicalSummary": "<summary for the clinical team emphasising patient context, constraints, and preferences>",
              "confidenceLevel": <number between 0.0 and 1.0>,
              "suggestedClinicianQuestions": ["<question 1>", "<question 2>"],
              "openQuestions": ["<unresolved concern 1>", "<unresolved concern 2>"]
            }
            """;

        var userMessage = $"""
            Workflow type: {context.WorkflowRequest.WorkflowType}
            Patient input: {context.WorkflowRequest.UserInput}
            Shared team notes:
            {context.SharedNotes}
            """;

        var json = await _llmClient.CompleteAsync(systemPrompt, userMessage, cancellationToken);
        return ParseResponse(json) ?? FallbackResponse();
    }

    private AgentResponse? ParseResponse(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<PatientResponseDto>(json, JsonOptions);
            if (dto is null)
            {
                return null;
            }

            return new AgentResponse
            {
                Role = Role,
                Summary = dto.Summary ?? string.Empty,
                TechnicalSummary = dto.TechnicalSummary ?? string.Empty,
                ConfidenceLevel = dto.ConfidenceLevel,
                SuggestedClinicianQuestions = dto.SuggestedClinicianQuestions ?? [],
                OpenQuestions = dto.OpenQuestions ?? []
            };
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private AgentResponse FallbackResponse() => new()
    {
        Role = Role,
        Summary = "Patient priorities and constraints captured. Please discuss with your care team.",
        TechnicalSummary = string.Empty,
        ConfidenceLevel = 0.50m,
        OpenQuestions = ["Response could not be parsed; please retry or consult your care team."]
    };

    private sealed class PatientResponseDto
    {
        public string? Summary { get; set; }
        public string? TechnicalSummary { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public List<string>? SuggestedClinicianQuestions { get; set; }
        public List<string>? OpenQuestions { get; set; }
    }
}
