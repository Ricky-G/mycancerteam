using MyCancerTeam.Core.Agents;

namespace MyCancerTeam.Core.Workflows;

public sealed class WorkflowRouter
{
    // The core multi-disciplinary team participates on every coordination so the patient
    // gets a true MDT view, not a narrow single-specialist answer.
    private static readonly AgentRole[] CoreTeam =
    [
        AgentRole.PatientOwner,
        AgentRole.MedicalOncologist,
        AgentRole.Radiologist,
        AgentRole.ResearchOncology,
        AgentRole.Psychologist
    ];

    public IReadOnlyList<AgentRole> GetRecommendedRoles(WorkflowRequest request)
    {
        var roles = new List<AgentRole>(CoreTeam);

        roles.AddRange(request.WorkflowType switch
        {
            WorkflowType.TravelAndPracticalSupport => [AgentRole.SocialWorker, AgentRole.AdminLogistics],
            WorkflowType.HomeSupport => [AgentRole.SocialWorker],
            WorkflowType.ImagingReview => [], // Radiologist already in core team
            WorkflowType.RadiationPlanReview => [AgentRole.RadiationOncologist],
            WorkflowType.MedicationPlanReview => [], // MedicalOncologist already in core team
            WorkflowType.SymptomSupport => [AgentRole.SocialWorker],
            WorkflowType.InsuranceAndFinancial => [AgentRole.FinancialAssistant, AgentRole.AdminLogistics],
            WorkflowType.ResearchMonitoring => [],
            WorkflowType.GlobalTreatmentAccess => [AgentRole.SocialWorker, AgentRole.AdminLogistics, AgentRole.FinancialAssistant],
            WorkflowType.DraftOutreach => [AgentRole.AdminLogistics, AgentRole.FinancialAssistant],
            WorkflowType.PhysicalFitness => [AgentRole.PhysicalFitness],
            _ => [AgentRole.RadiationOncologist, AgentRole.SpecialistSurgeon]
        });

        return roles.Distinct().ToList();
    }
}
