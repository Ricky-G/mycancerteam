using Azure.Core;

namespace MyCancerTeam.Infrastructure.Azure;

public sealed class AzureOpenAiClientContext
{
    public required Uri Endpoint { get; init; }
    public required string DeploymentName { get; init; }
    public required TokenCredential Credential { get; init; }
}
