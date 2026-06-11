using MyCancerTeam.Core.Agents;

namespace MyCancerTeam.Core.Workflows;

public sealed class WorkflowRouter
{
    public IReadOnlyList<AgentRole> GetRecommendedRoles(WorkflowRequest request)
    {
        var roles = new List<AgentRole>
        {
            AgentRole.PatientOwner,
            AgentRole.ResearchOncology
        };

        roles.AddRange(request.WorkflowType switch
        {
            WorkflowType.TravelAndPracticalSupport => [AgentRole.SocialWorker, AgentRole.AdminLogistics],
            WorkflowType.HomeSupport => [AgentRole.SocialWorker, AgentRole.Psychologist],
            WorkflowType.ImagingReview => [AgentRole.Radiologist],
            WorkflowType.RadiationPlanReview => [AgentRole.RadiationOncologist],
            WorkflowType.MedicationPlanReview => [AgentRole.MedicalOncologist],
            WorkflowType.SymptomSupport => [AgentRole.MedicalOncologist, AgentRole.Psychologist],
            WorkflowType.InsuranceAndFinancial => [AgentRole.FinancialAssistant, AgentRole.AdminLogistics],
            WorkflowType.ResearchMonitoring => [AgentRole.ResearchOncology],
            WorkflowType.GlobalTreatmentAccess => [AgentRole.ResearchOncology, AgentRole.SocialWorker, AgentRole.AdminLogistics],
            WorkflowType.DraftOutreach => [AgentRole.AdminLogistics, AgentRole.FinancialAssistant],
            WorkflowType.PhysicalFitness => [AgentRole.PhysicalFitness, AgentRole.Psychologist],
            _ => [AgentRole.MedicalOncologist]
        });

        return roles.Distinct().ToList();
    }
}
