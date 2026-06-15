namespace MyCancerTeam.Core.AI;

/// <summary>
/// Sends a single-turn chat request to a language model and returns the text response.
/// </summary>
/// <remarks>
/// TODO: Support additional authentication mechanisms beyond DefaultAzureCredential.
/// </remarks>
public interface ILlmChatClient
{
    Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);

    Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default);
}
