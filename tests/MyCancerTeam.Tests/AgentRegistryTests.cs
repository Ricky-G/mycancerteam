using MyCancerTeam.Core.Agents;
using MyCancerTeam.Infrastructure.Research;

namespace MyCancerTeam.Tests;

public sealed class AgentRegistryTests
{
    [Fact]
    public void Registry_ShouldRegisterGeneralAndResearchOncologyAgents()
    {
        var registry = new AgentRegistry();
        registry.Register(new PatientOwnerAgent());
        registry.Register(new ResearchOncologyAgent(new ResearchOncologyService()));

        Assert.NotNull(registry.Get(AgentRole.PatientOwner));
        Assert.NotNull(registry.Get(AgentRole.ResearchOncology));
        Assert.IsAssignableFrom<IResearchOncologyAgent>(registry.Get(AgentRole.ResearchOncology));
    }
}
