using System.Text.Json;
using MyCancerTeam.Core.AI;

namespace MyCancerTeam.Core.Agents;

public sealed class SpecialistAgent : ISpecialistAgent
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ILlmChatClient _llmClient;

    public SpecialistAgent(AgentRole role, string name, ILlmChatClient llmClient)
    {
        Role = role;
        Name = name;
        _llmClient = llmClient;
    }

    public AgentRole Role { get; }
    public string Name { get; }

    public bool CanHandle(AgentContext context) => true;

    public async Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        var systemPrompt = BuildSystemPrompt(Role);
        var userMessage = BuildUserMessage(context);
        var json = await _llmClient.CompleteAsync(systemPrompt, userMessage, cancellationToken);

        return ParseResponse(json) ?? FallbackResponse();
    }

    private static string BuildSystemPrompt(AgentRole role)
    {
        var roleDescription = GetRoleDescription(role);
        return $$"""
            You are an AI clinical support agent specialising in {{roleDescription}}, helping a cancer patient and their support team understand their care.

            Always distinguish facts from assumptions and uncertainties. Recommend professional medical consultation for all major decisions.

            Respond with a JSON object containing exactly these fields:
            {
              "summary": "<patient-friendly summary in 2-4 sentences>",
              "technicalSummary": "<clinical or technical summary for healthcare providers>",
              "confidenceLevel": <number between 0.0 and 1.0>,
              "suggestedClinicianQuestions": ["<question 1>", "<question 2>", "<question 3>"],
              "openQuestions": ["<uncertainty or gap 1>", "<uncertainty or gap 2>"]
            }
            """;
    }

    private static string BuildUserMessage(AgentContext context) =>
        $"""
        Workflow type: {context.WorkflowRequest.WorkflowType}
        Patient input: {context.WorkflowRequest.UserInput}
        Shared team notes:
        {context.SharedNotes}
        """;

    private AgentResponse? ParseResponse(string json)
    {
        try
        {
            var dto = JsonSerializer.Deserialize<SpecialistResponseDto>(json, JsonOptions);
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
        Summary = $"{Name} analysis prepared. Please consult your clinician for guidance.",
        TechnicalSummary = string.Empty,
        ConfidenceLevel = 0.30m,
        OpenQuestions = ["Response could not be parsed; please retry or consult your care team."]
    };

    private static string GetRoleDescription(AgentRole role) => role switch
    {
        AgentRole.RadiationOncologist => "radiation oncology",
        AgentRole.MedicalOncologist => "medical oncology",
        AgentRole.Radiologist => "radiology and imaging interpretation",
        AgentRole.SpecialistSurgeon => "specialist surgical oncology",
        AgentRole.Psychologist => "psycho-oncology and emotional support",
        AgentRole.FinancialAssistant => "cancer care financial navigation",
        AgentRole.SocialWorker => "cancer care social work and navigation",
        AgentRole.AdminLogistics => "medical administration and care logistics",
        _ => role.ToString()
    };

    private sealed class SpecialistResponseDto
    {
        public string? Summary { get; set; }
        public string? TechnicalSummary { get; set; }
        public decimal ConfidenceLevel { get; set; }
        public List<string>? SuggestedClinicianQuestions { get; set; }
        public List<string>? OpenQuestions { get; set; }
    }
}
