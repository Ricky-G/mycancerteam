using MyCancerTeam.Core.Agents;
using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Tests;

public sealed class WorkflowRouterTests
{
    [Fact]
    public void WorkflowRouter_ShouldRouteInsuranceToFinancialAndAdminAgents()
    {
        var router = new WorkflowRouter();
        var roles = router.GetRecommendedRoles(new WorkflowRequest
        {
            WorkflowType = WorkflowType.InsuranceAndFinancial,
            UserInput = "insurance help"
        });

        Assert.Contains(AgentRole.FinancialAssistant, roles);
        Assert.Contains(AgentRole.AdminLogistics, roles);
        Assert.Contains(AgentRole.ResearchOncology, roles);
    }
}
