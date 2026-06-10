namespace MyCancerTeam.Core.Agents;

public interface IAgent
{
    AgentRole Role { get; }
    string Name { get; }
    bool CanHandle(AgentContext context);
    Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default);
}
