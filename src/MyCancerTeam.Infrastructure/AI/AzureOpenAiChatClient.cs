using Azure.AI.OpenAI;
using MyCancerTeam.Core.AI;
using MyCancerTeam.Infrastructure.Azure;
using OpenAI.Chat;

namespace MyCancerTeam.Infrastructure.AI;

public sealed class AzureOpenAiChatClient : ILlmChatClient
{
    private readonly ChatClient _chatClient;

    public AzureOpenAiChatClient(AzureOpenAiClientContext context)
    {
        var openAiClient = new AzureOpenAIClient(context.Endpoint, context.Credential);
        _chatClient = openAiClient.GetChatClient(context.DeploymentName);
    }

    public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        => CompleteInternalAsync(systemPrompt, userMessage, ChatResponseFormat.CreateJsonObjectFormat(), cancellationToken);

    public Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        => CompleteInternalAsync(systemPrompt, userMessage, ChatResponseFormat.CreateTextFormat(), cancellationToken);

    private async Task<string> CompleteInternalAsync(string systemPrompt, string userMessage, ChatResponseFormat responseFormat, CancellationToken cancellationToken)
    {
        var options = new ChatCompletionOptions
        {
            ResponseFormat = responseFormat
        };

        var completion = await _chatClient.CompleteChatAsync(
            [new SystemChatMessage(systemPrompt), new UserChatMessage(userMessage)],
            options,
            cancellationToken);

        var content = completion.Value.Content;
        if (content.Count == 0)
        {
            return string.Empty;
        }

        var builder = new System.Text.StringBuilder();
        foreach (var part in content)
        {
            builder.Append(part.Text);
        }

        return builder.ToString();
    }
}
