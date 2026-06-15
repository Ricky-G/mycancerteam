using MyCancerTeam.Core.AI;

namespace MyCancerTeam.Tests.Helpers;

/// <summary>
/// A deterministic stub for <see cref="ILlmChatClient"/> that returns a fixed valid JSON response
/// without making any real network calls. Suitable for unit tests only.
/// </summary>
internal sealed class StubLlmChatClient : ILlmChatClient
{
    public static readonly StubLlmChatClient Instance = new();

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
    {
        var json = """
            {
              "summary": "Stub summary for testing.",
              "technicalSummary": "Stub technical summary.",
              "confidenceLevel": 0.5,
              "suggestedClinicianQuestions": ["Stub question?"],
              "openQuestions": ["Stub open question."],
              "patientFriendlySummary": "Stub patient-friendly summary.",
              "suggestedQuestions": ["Stub suggested question?"],
              "evidenceGapNotes": ["Stub gap note."],
              "draftContent": "Stub draft content. Safety and Validation Note: consult professionals."
            }
            """;

        return Task.FromResult(json);
    }

    public Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        => Task.FromResult($"Stub formatted output for: {userMessage}");
}
