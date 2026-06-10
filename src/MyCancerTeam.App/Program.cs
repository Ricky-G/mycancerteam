using System.Text;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Core.Drafts;
using MyCancerTeam.Core.Workflows;
using MyCancerTeam.Infrastructure.Configuration;
using MyCancerTeam.Infrastructure.Drafts;
using MyCancerTeam.Infrastructure.Notes;
using MyCancerTeam.Infrastructure.Research;

var rootPath = ResolveRepositoryRoot();
var configurationLoader = new ConfigurationLoader();
var configuration = configurationLoader.Load(rootPath);
configurationLoader.EnsureLocalDirectories(configuration);

var noteStore = new MarkdownNoteStore(configuration);
var draftExporter = new MarkdownDraftExporter(configuration);
var draftService = new DraftCommunicationService(draftExporter);
var researchService = new ResearchOncologyService();

var registry = new AgentRegistry();
registry.Register(new PatientOwnerAgent());
registry.Register(new ResearchOncologyAgent(researchService));
registry.Register(new SpecialistAgent(AgentRole.RadiationOncologist, "Radiation Oncologist Agent"));
registry.Register(new SpecialistAgent(AgentRole.MedicalOncologist, "Medical Oncologist Agent"));
registry.Register(new SpecialistAgent(AgentRole.Radiologist, "Radiologist Agent"));
registry.Register(new SpecialistAgent(AgentRole.SpecialistSurgeon, "Specialist Surgeon Agent"));
registry.Register(new SpecialistAgent(AgentRole.Psychologist, "Psychologist / Emotional Support Agent"));
registry.Register(new SpecialistAgent(AgentRole.FinancialAssistant, "Financial Assistant Agent"));
registry.Register(new SpecialistAgent(AgentRole.SocialWorker, "Social Worker / Care Navigation Agent"));
registry.Register(new SpecialistAgent(AgentRole.AdminLogistics, "Admin / Logistics Agent"));

var teamLeadAgent = new TeamLeadAgent(registry, new WorkflowRouter());
registry.Register(teamLeadAgent);

var sharedNotes = await noteStore.ReadSharedNotesAsync();
var userInput = args.Length > 0
    ? string.Join(' ', args)
    : Prompt("Enter a short update/question (or type 'draft' for a communication draft): ");

if (string.Equals(userInput.Trim(), "draft", StringComparison.OrdinalIgnoreCase))
{
    await HandleDraftAsync(draftService);
    return;
}

var request = new WorkflowRequest
{
    WorkflowType = InferWorkflowType(userInput),
    UserInput = userInput
};

var response = await teamLeadAgent.CoordinateAsync(request, sharedNotes);

Console.WriteLine();
Console.WriteLine("=== Team Lead Summary ===");
Console.WriteLine(response.Summary);
Console.WriteLine($"Confidence: {response.ConfidenceLevel:P0}");

if (response.SuggestedClinicianQuestions.Count > 0)
{
    Console.WriteLine();
    Console.WriteLine("Suggested Clinician Questions:");
    foreach (var question in response.SuggestedClinicianQuestions)
    {
        Console.WriteLine($"- {question}");
    }
}

var updatedNotes = new StringBuilder();
if (!string.IsNullOrWhiteSpace(sharedNotes))
{
    updatedNotes.AppendLine(sharedNotes.Trim());
    updatedNotes.AppendLine();
}

updatedNotes.AppendLine($"## Update {DateTimeOffset.UtcNow:O}");
updatedNotes.AppendLine($"User input: {userInput}");
updatedNotes.AppendLine();
updatedNotes.AppendLine("### Team Lead Summary");
updatedNotes.AppendLine(response.Summary);

if (response.OpenQuestions.Count > 0)
{
    updatedNotes.AppendLine();
    updatedNotes.AppendLine("### Open Questions");
    foreach (var openQuestion in response.OpenQuestions)
    {
        updatedNotes.AppendLine($"- {openQuestion}");
    }
}

await noteStore.WriteSharedNotesAsync(updatedNotes.ToString());
Console.WriteLine();
Console.WriteLine($"Shared notes updated at: {configuration.LatestSharedNotesPath}");

static string ResolveRepositoryRoot()
{
    var current = AppContext.BaseDirectory;
    return Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
}

static string Prompt(string prompt)
{
    Console.Write(prompt);
    return Console.ReadLine() ?? string.Empty;
}

static WorkflowType InferWorkflowType(string input)
{
    var normalized = input.ToLowerInvariant();

    if (normalized.Contains("travel") || normalized.Contains("visa") || normalized.Contains("transport"))
    {
        return WorkflowType.TravelAndPracticalSupport;
    }

    if (normalized.Contains("imaging") || normalized.Contains("scan") || normalized.Contains("mri") || normalized.Contains("ct") || normalized.Contains("pet"))
    {
        return WorkflowType.ImagingReview;
    }

    if (normalized.Contains("radiation") || normalized.Contains("fraction") || normalized.Contains("proton") || normalized.Contains("photon"))
    {
        return WorkflowType.RadiationPlanReview;
    }

    if (normalized.Contains("chemo") || normalized.Contains("medication") || normalized.Contains("systemic"))
    {
        return WorkflowType.MedicationPlanReview;
    }

    if (normalized.Contains("insurance") || normalized.Contains("claim") || normalized.Contains("reimburse"))
    {
        return WorkflowType.InsuranceAndFinancial;
    }

    if (normalized.Contains("trial") || normalized.Contains("research") || normalized.Contains("evidence"))
    {
        return WorkflowType.ResearchMonitoring;
    }

    if (normalized.Contains("international") || normalized.Contains("overseas") || normalized.Contains("second opinion"))
    {
        return WorkflowType.GlobalTreatmentAccess;
    }

    if (normalized.Contains("symptom") || normalized.Contains("nausea") || normalized.Contains("fatigue") || normalized.Contains("pain"))
    {
        return WorkflowType.SymptomSupport;
    }

    return WorkflowType.GeneralUpdate;
}

static async Task HandleDraftAsync(DraftCommunicationService draftService)
{
    var type = Prompt("Draft type (emails/insurance/second-opinions/trials): ");
    var recipient = Prompt("Recipient type (clinician/hospital/insurer/trial-coordinator/etc): ");
    var subject = Prompt("Subject: ");
    var context = Prompt("Patient context summary: ");
    var details = Prompt("Additional details (optional): ");

    var result = await draftService.CreateDraftAsync(new DraftCommunicationRequest
    {
        DraftType = string.IsNullOrWhiteSpace(type) ? "emails" : type,
        RecipientType = recipient,
        Subject = subject,
        PatientContextSummary = context,
        AdditionalDetails = details
    });

    Console.WriteLine();
    Console.WriteLine($"Draft created: {result.FilePath}");
}
