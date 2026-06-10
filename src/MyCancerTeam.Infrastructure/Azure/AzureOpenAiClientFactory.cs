using Azure.Identity;
using MyCancerTeam.Core.Configuration;

namespace MyCancerTeam.Infrastructure.Azure;

public sealed class AzureOpenAiClientFactory
{
    public AzureOpenAiClientContext Create(AppConfiguration configuration)
    {
        if (string.IsNullOrWhiteSpace(configuration.AzureOpenAiEndpoint) ||
            string.IsNullOrWhiteSpace(configuration.AzureOpenAiDeployment))
        {
            throw new InvalidOperationException("Azure OpenAI endpoint and deployment must be configured.");
        }

        return new AzureOpenAiClientContext
        {
            Endpoint = new Uri(configuration.AzureOpenAiEndpoint),
            DeploymentName = configuration.AzureOpenAiDeployment,
            Credential = new DefaultAzureCredential()
        };
    }
}
