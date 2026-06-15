using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Tests;

public sealed class TeamLeadAgentSynthesisTests
{
    [Fact]
    public async Task CoordinateAsync_WithLlm_ProducesUnifiedSynthesisInsteadOfPerAgentBullets()
    {
        var registry = new AgentRegistry();
        registry.Register(new StubSpecialist(AgentRole.PatientOwner, "Patient Owner Agent",
            "Patient is anxious about next steps.", "Patient context noted."));
        registry.Register(new StubSpecialist(AgentRole.MedicalOncologist, "Medical Oncologist Agent",
            "Adjuvant chemo recommended.", "Adjuvant FEC-T discussed; pending pathology."));
        registry.Register(new StubSpecialist(AgentRole.Radiologist, "Radiologist Agent",
            "Imaging shows stable disease.", "MRI demonstrates no new lesions."));
        registry.Register(new StubSpecialist(AgentRole.ResearchOncology, "Research Oncology Agent",
            "Evidence supports current plan.", "NCCN-aligned regimen."));
        registry.Register(new StubSpecialist(AgentRole.Psychologist, "Psychologist Agent",
            "Patient would benefit from counselling.", "Refer to psycho-oncology."));

        var llm = new SynthesisingChatClient("""
            {
              "currentDiagnosis": "Stage II breast cancer with stable imaging and confirmed pathology under MDT review.",
              "currentTreatment": "Adjuvant chemotherapy planned per NCCN guidance, alongside psycho-oncology referral.",
              "openQuestions": ["Confirm final pathology grade."],
              "clinicianQuestions": ["Should adjuvant chemo start within 4 weeks?"],
              "confidenceLevel": 0.8
            }
            """);

        var teamLead = new TeamLeadAgent(registry, new WorkflowRouter(), llm);

        var response = await teamLead.CoordinateAsync(
            new WorkflowRequest { WorkflowType = WorkflowType.GeneralUpdate, UserInput = "Latest MRI in." },
            sharedNotes: string.Empty);

        Assert.Equal(
            "Stage II breast cancer with stable imaging and confirmed pathology under MDT review.",
            response.Summary);
        Assert.Equal(
            "Adjuvant chemotherapy planned per NCCN guidance, alongside psycho-oncology referral.",
            response.TechnicalSummary);
        Assert.DoesNotContain("- PatientOwner:", response.Summary);
        Assert.DoesNotContain("- MedicalOncologist:", response.TechnicalSummary);
        Assert.Equal(0.8m, response.ConfidenceLevel);
        Assert.Contains("Should adjuvant chemo start within 4 weeks?", response.SuggestedClinicianQuestions);
        Assert.NotEmpty(response.EngagedAgents);
        Assert.Contains("Latest MRI in.", llm.LastUserMessage);
    }

    [Fact]
    public async Task CoordinateAsync_WithoutLlm_FallsBackToAggregatedBullets()
    {
        var registry = new AgentRegistry();
        registry.Register(new StubSpecialist(AgentRole.PatientOwner, "Patient Owner Agent",
            "Patient summary.", "Patient technical."));
        registry.Register(new StubSpecialist(AgentRole.MedicalOncologist, "Medical Oncologist Agent",
            "Med onc summary.", "Med onc technical."));
        registry.Register(new StubSpecialist(AgentRole.Radiologist, "Radiologist Agent",
            "Imaging summary.", "Imaging technical."));
        registry.Register(new StubSpecialist(AgentRole.ResearchOncology, "Research Oncology Agent",
            "Research summary.", "Research technical."));
        registry.Register(new StubSpecialist(AgentRole.Psychologist, "Psychologist Agent",
            "Psych summary.", "Psych technical."));

        var teamLead = new TeamLeadAgent(registry, new WorkflowRouter());

        var response = await teamLead.CoordinateAsync(
            new WorkflowRequest { WorkflowType = WorkflowType.GeneralUpdate, UserInput = "Update." },
            sharedNotes: string.Empty);

        Assert.Contains("PatientOwner: Patient summary.", response.Summary);
        Assert.Contains("MedicalOncologist: Med onc technical.", response.TechnicalSummary);
    }

    private sealed class StubSpecialist : IAgent
    {
        private readonly string _summary;
        private readonly string _technical;

        public StubSpecialist(AgentRole role, string name, string summary, string technical)
        {
            Role = role;
            Name = name;
            _summary = summary;
            _technical = technical;
        }

        public AgentRole Role { get; }
        public string Name { get; }
        public bool CanHandle(AgentContext context) => true;

        public Task<AgentResponse> RespondAsync(AgentContext context, CancellationToken cancellationToken = default)
            => Task.FromResult(new AgentResponse
            {
                Role = Role,
                Summary = _summary,
                TechnicalSummary = _technical,
                ConfidenceLevel = 0.5m
            });
    }

    private sealed class SynthesisingChatClient : ILlmChatClient
    {
        private readonly string _jsonResponse;

        public SynthesisingChatClient(string jsonResponse)
        {
            _jsonResponse = jsonResponse;
        }

        public string LastUserMessage { get; private set; } = string.Empty;

        public Task<string> CompleteAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
        {
            LastUserMessage = userMessage;
            return Task.FromResult(_jsonResponse);
        }

        public Task<string> CompleteTextAsync(string systemPrompt, string userMessage, CancellationToken cancellationToken = default)
            => throw new NotSupportedException();
    }
}
