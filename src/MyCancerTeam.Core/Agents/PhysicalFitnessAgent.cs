namespace MyCancerTeam.Core.Agents;

public sealed class PhysicalFitnessAgent : ISpecialistAgent
{
    public AgentRole Role => AgentRole.PhysicalFitness;
    public string Name => "Physical Fitness Agent";

    public bool CanHandle(AgentContext context) => true;

    public Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(new AgentResponse
        {
            Role = Role,
            Summary = "Gradual, condition-appropriate exercise plan prepared. Light to moderate activity can reduce cancer-related fatigue, improve mood, and support physical recovery when paced to the patient's current capacity.",
            TechnicalSummary = "Exercise recommendations are tailored to treatment phase, fatigue level, and any musculoskeletal or cardiovascular contraindications. Aerobic, resistance, and flexibility components are balanced incrementally.",
            ConfidenceLevel = 0.60m,
            SuggestedClinicianQuestions =
            [
                "Are there any current contraindications to exercise (e.g., bone metastases, thrombocytopenia, severe anaemia, cardiac risk)?",
                "Has the patient been assessed by a physiotherapist or exercise physiologist during or after treatment?",
                "What is the patient's current fatigue level and baseline activity tolerance?"
            ],
            OpenQuestions =
            [
                "Confirm exercise safety thresholds given current blood counts and treatment side effects.",
                "Determine whether supervised exercise (physio/gym) or home-based programme is more appropriate.",
                "Assess patient motivation and any barriers to physical activity (pain, transport, carer demands)."
            ]
        });
    }
}
