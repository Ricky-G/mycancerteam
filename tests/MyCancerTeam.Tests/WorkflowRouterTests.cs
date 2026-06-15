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

    [Fact]
    public void WorkflowRouter_GeneralUpdate_ShouldEngageCoreMultidisciplinaryTeam()
    {
        var router = new WorkflowRouter();
        var roles = router.GetRecommendedRoles(new WorkflowRequest
        {
            WorkflowType = WorkflowType.GeneralUpdate,
            UserInput = "general update"
        });

        Assert.Contains(AgentRole.PatientOwner, roles);
        Assert.Contains(AgentRole.MedicalOncologist, roles);
        Assert.Contains(AgentRole.Radiologist, roles);
        Assert.Contains(AgentRole.ResearchOncology, roles);
        Assert.Contains(AgentRole.Psychologist, roles);
        Assert.Contains(AgentRole.RadiationOncologist, roles);
        Assert.Contains(AgentRole.SpecialistSurgeon, roles);
    }

    [Fact]
    public void WorkflowRouter_ImagingReview_ShouldStillIncludeCoreTeam()
    {
        var router = new WorkflowRouter();
        var roles = router.GetRecommendedRoles(new WorkflowRequest
        {
            WorkflowType = WorkflowType.ImagingReview,
            UserInput = "review mri"
        });

        Assert.Contains(AgentRole.Radiologist, roles);
        Assert.Contains(AgentRole.MedicalOncologist, roles);
        Assert.Contains(AgentRole.Psychologist, roles);
        Assert.Contains(AgentRole.PatientOwner, roles);
        Assert.Contains(AgentRole.ResearchOncology, roles);
    }
}
