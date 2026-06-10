namespace MyCancerTeam.Core.Agents;

public interface IAgentRegistry
{
    void Register(IAgent agent);
    IAgent? Get(AgentRole role);
    IReadOnlyCollection<IAgent> GetAll();
}
