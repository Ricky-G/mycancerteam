using MyCancerTeam.App;
using MyCancerTeam.Core.Agents;
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
var scanner = new FolderNoteScanner(configuration);

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
registry.Register(new PhysicalFitnessAgent());

var teamLeadAgent = new TeamLeadAgent(registry, new WorkflowRouter());
registry.Register(teamLeadAgent);

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true; // Prevent abrupt termination so shutdown can be graceful.
    cts.Cancel();
};

var initialInput = args.Length > 0 ? string.Join(' ', args) : null;

var host = new InteractiveSessionHost(noteStore, teamLeadAgent, draftService, scanner, configuration);
await host.RunAsync(cts, initialInput);

static string ResolveRepositoryRoot()
{
    var current = AppContext.BaseDirectory;
    return Path.GetFullPath(Path.Combine(current, "..", "..", "..", "..", ".."));
}
