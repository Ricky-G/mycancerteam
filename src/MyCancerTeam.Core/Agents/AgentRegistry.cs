namespace MyCancerTeam.Core.Agents;

public sealed class AgentRegistry : IAgentRegistry
{
    private readonly Dictionary<AgentRole, IAgent> _agents = new();

    public void Register(IAgent agent)
    {
        _agents[agent.Role] = agent;
    }

    public IAgent? Get(AgentRole role)
    {
        _agents.TryGetValue(role, out var agent);
        return agent;
    }

    public IReadOnlyCollection<IAgent> GetAll() => _agents.Values.ToList();
}
