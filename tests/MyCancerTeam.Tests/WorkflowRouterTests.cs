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
    }

    [Fact]
    public void WorkflowRouter_ShouldRoutePhysicalFitnessToFitnessAndPsychologyAgents()
    {
        var router = new WorkflowRouter();
        var roles = router.GetRecommendedRoles(new WorkflowRequest
        {
            WorkflowType = WorkflowType.PhysicalFitness,
            UserInput = "exercise plan"
        });

        Assert.Contains(AgentRole.PhysicalFitness, roles);
        Assert.Contains(AgentRole.Psychologist, roles);
    }
}
