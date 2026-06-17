using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Channels;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Drafts;
using MyCancerTeam.Core.Notes;
using MyCancerTeam.Core.Workflows;
using MyCancerTeam.Infrastructure.Notes;

namespace MyCancerTeam.App;

/// <summary>
/// Runs the application as a continuous, concurrent loop: an interactive prompt and a
/// background folder watcher both feed a single sequential processing pipeline.
/// </summary>
public sealed class InteractiveSessionHost
{
    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(5);
    private static readonly Regex RolePrefixPattern = new(@"^[A-Z][A-Za-z]+:\s+", RegexOptions.Compiled);

    private readonly INoteStore _noteStore;
    private readonly ITeamLeadAgent _teamLeadAgent;
    private readonly IDraftCommunicationService _draftService;
    private readonly IFolderNoteScanner _scanner;
    private readonly TeamLeadSummaryComposer _summaryComposer;
    private readonly AppConfiguration _configuration;
    private readonly TimeSpan _pollInterval;
    private readonly object _consoleLock = new();

    public InteractiveSessionHost(
        INoteStore noteStore,
        ITeamLeadAgent teamLeadAgent,
        IDraftCommunicationService draftService,
        IFolderNoteScanner scanner,
        TeamLeadSummaryComposer summaryComposer,
        AppConfiguration configuration,
        TimeSpan? pollInterval = null)
    {
        _noteStore = noteStore;
        _teamLeadAgent = teamLeadAgent;
        _draftService = draftService;
        _scanner = scanner;
        _summaryComposer = summaryComposer;
        _configuration = configuration;
        _pollInterval = pollInterval ?? DefaultPollInterval;
    }

    public async Task RunAsync(CancellationTokenSource cancellationSource, string? initialInput = null)
    {
        var channel = Channel.CreateUnbounded<WorkItem>(new UnboundedChannelOptions
        {
            SingleReader = true
        });

        PrintWelcome();

        Log($"Watching {_scanner.WatchedFolders.Count} folder(s) for notes. Existing notes are analyzed on first run; newly added notes are processed as they arrive.");

        if (!string.IsNullOrWhiteSpace(initialInput))
        {
            await channel.Writer.WriteAsync(
                new WorkItem(WorkItemKind.UserQuery, initialInput.Trim(), "Command-line input"),
                cancellationSource.Token);
        }

        PrintPrompt();

        var inputTask = Task.Run(() => ReadUserInputAsync(channel.Writer, cancellationSource));
        var watchTask = Task.Run(() => WatchFoldersAsync(channel.Writer, cancellationSource.Token));

        try
        {
            await ConsumeAsync(channel.Reader, cancellationSource.Token);
        }
        finally
        {
            channel.Writer.TryComplete();
        }

        // The watcher observes cancellation cooperatively and returns promptly.
        await watchTask;

        // The input reader may be parked on a blocking Console.ReadLine(); do not hang
        // shutdown waiting for the next keystroke.
        await Task.WhenAny(inputTask, Task.Delay(TimeSpan.FromMilliseconds(250)));

        Log("Session ended.");
    }

    private async Task ReadUserInputAsync(ChannelWriter<WorkItem> writer, CancellationTokenSource cancellationSource)
    {
        try
        {
            while (!cancellationSource.IsCancellationRequested)
            {
                var line = await Task.Run(Console.ReadLine);
                if (cancellationSource.IsCancellationRequested)
                {
                    break;
                }

                if (line is null)
                {
                    // End of the input stream (Ctrl+Z or redirected input finished).
                    cancellationSource.Cancel();
                    break;
                }

                var trimmed = line.Trim();
                if (trimmed.Length == 0)
                {
                    PrintPrompt();
                    continue;
                }

                if (IsExitCommand(trimmed))
                {
                    cancellationSource.Cancel();
                    break;
                }

                if (string.Equals(trimmed, "help", StringComparison.OrdinalIgnoreCase))
                {
                    PrintHelp();
                    PrintPrompt();
                    continue;
                }

                if (string.Equals(trimmed, "draft", StringComparison.OrdinalIgnoreCase))
                {
                    await HandleDraftAsync(cancellationSource.Token);
                    PrintPrompt();
                    continue;
                }

                await writer.WriteAsync(
                    new WorkItem(WorkItemKind.UserQuery, trimmed, "Interactive input"),
                    cancellationSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
        }
        finally
        {
            writer.TryComplete();
        }
    }

    private async Task WatchFoldersAsync(ChannelWriter<WorkItem> writer, CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                var newNotes = await _scanner.ScanForNewNotesAsync(cancellationToken);
                foreach (var note in newNotes)
                {
                    if (note.RequiresOcr)
                    {
                        Log($"Skipped note with no extractable text (looks scanned/image-only; OCR not yet supported): {note.FilePath}");
                        continue;
                    }

                    Log($"New note detected: {note.FilePath}");
                    await writer.WriteAsync(
                        new WorkItem(WorkItemKind.FileNote, note.Content, $"File: {note.FilePath}"),
                        cancellationToken);
                }

                await Task.Delay(_pollInterval, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ChannelClosedException)
        {
        }
    }

    private async Task ConsumeAsync(ChannelReader<WorkItem> reader, CancellationToken cancellationToken)
    {
        try
        {
            await foreach (var item in reader.ReadAllAsync(cancellationToken))
            {
                await ProcessAsync(item, cancellationToken);
                PrintPrompt();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ProcessAsync(WorkItem item, CancellationToken cancellationToken)
    {
        // The previous summary is the carried-forward state of the case; the shared notes file is
        // an append-only audit log. New information is analyzed in conjunction with the previous
        // summary, the analysis is appended to the audit log, and the summary is regenerated.
        var previousSummary = await _noteStore.ReadSummaryAsync(cancellationToken);
        var auditLog = await _noteStore.ReadSharedNotesAsync(cancellationToken);

        var request = new WorkflowRequest
        {
            WorkflowType = InferWorkflowType(item.Input),
            UserInput = item.Input
        };

        var response = await _teamLeadAgent.CoordinateAsync(request, previousSummary, cancellationToken);

        PrintResponse(item, response);

        var updatedNotes = BuildUpdatedNotes(auditLog, item, response);
        await _noteStore.WriteSharedNotesAsync(updatedNotes, cancellationToken);

        Log($"Shared notes updated at: {_configuration.SharedNotesFilePath}");

        var summary = await _summaryComposer.ComposeAsync(previousSummary, item.Source, item.Input, response, cancellationToken);
        await _noteStore.WriteSummaryAsync(summary, cancellationToken);

        Log($"Summary updated at: {_configuration.SummaryFilePath}");
    }

    private async Task HandleDraftAsync(CancellationToken cancellationToken)
    {
        var type = Prompt("Draft type (emails/insurance/second-opinions/trials): ");
        var recipient = Prompt("Recipient type (clinician/hospital/insurer/trial-coordinator/etc): ");
        var subject = Prompt("Subject: ");
        var context = Prompt("Patient context summary: ");
        var details = Prompt("Additional details (optional): ");

        var result = await _draftService.CreateDraftAsync(new DraftCommunicationRequest
        {
            DraftType = string.IsNullOrWhiteSpace(type) ? "emails" : type,
            RecipientType = recipient,
            Subject = subject,
            PatientContextSummary = context,
            AdditionalDetails = details
        }, cancellationToken);

        Log($"Draft created: {result.FilePath}");
    }

    private void PrintResponse(WorkItem item, AgentResponse response)
    {
        lock (_consoleLock)
        {
            Console.WriteLine();
            Console.WriteLine($"=== Team Lead Summary ({item.Source}) ===");
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
        }
    }

    private static string BuildUpdatedNotes(string sharedNotes, WorkItem item, AgentResponse response)
    {
        var updatedNotes = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(sharedNotes))
        {
            updatedNotes.AppendLine(sharedNotes.Trim());
            updatedNotes.AppendLine();
        }

        updatedNotes.AppendLine($"## Update {DateTimeOffset.UtcNow:O}");
        updatedNotes.AppendLine($"Source: {item.Source}");
        updatedNotes.AppendLine($"User input: {item.Input}");
        updatedNotes.AppendLine($"Confidence: {response.ConfidenceLevel:P0}");
        updatedNotes.AppendLine();

        updatedNotes.AppendLine("### Current Diagnosis");
        updatedNotes.AppendLine(string.IsNullOrWhiteSpace(response.Summary) ? "Not yet established." : response.Summary.Trim());
        updatedNotes.AppendLine();

        updatedNotes.AppendLine("### Current Treatment");
        updatedNotes.AppendLine(string.IsNullOrWhiteSpace(response.TechnicalSummary) ? "Not yet established." : response.TechnicalSummary.Trim());

        var nextSteps = response.SuggestedClinicianQuestions
            .Concat(response.OpenQuestions)
            .Where(step => !string.IsNullOrWhiteSpace(step))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (nextSteps.Count > 0)
        {
            updatedNotes.AppendLine();
            updatedNotes.AppendLine("### Next Steps");
            foreach (var nextStep in nextSteps)
            {
                updatedNotes.AppendLine($"- {nextStep}");
            }
        }

        if (response.OpenQuestions.Count > 0)
        {
            updatedNotes.AppendLine();
            updatedNotes.AppendLine("### Open Questions");
            foreach (var openQuestion in response.OpenQuestions)
            {
                updatedNotes.AppendLine($"- {openQuestion}");
            }
        }

        if (response.EngagedAgents.Count > 0)
        {
            updatedNotes.AppendLine();
            updatedNotes.AppendLine("### Engaged Agents");
            foreach (var agent in response.EngagedAgents)
            {
                updatedNotes.AppendLine($"- {agent}");
            }
        }

        return updatedNotes.ToString();
    }

    private static WorkflowType InferWorkflowType(string input)
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

        if (normalized.Contains("exercise") || normalized.Contains("fitness") || normalized.Contains("physical activity")
            || normalized.Contains("workout") || normalized.Contains("walking") || normalized.Contains("physio")
            || normalized.Contains("rehabilitation") || normalized.Contains("rehab"))
        {
            return WorkflowType.PhysicalFitness;
        }

        return WorkflowType.GeneralUpdate;
    }

    private static bool IsExitCommand(string input)
        => string.Equals(input, "exit", StringComparison.OrdinalIgnoreCase)
           || string.Equals(input, "quit", StringComparison.OrdinalIgnoreCase);

    private string Prompt(string prompt)
    {
        lock (_consoleLock)
        {
            Console.Write(prompt);
        }

        return Console.ReadLine() ?? string.Empty;
    }

    private void Log(string message)
    {
        lock (_consoleLock)
        {
            Console.WriteLine(message);
        }
    }

    private void PrintPrompt()
    {
        lock (_consoleLock)
        {
            Console.Write("> ");
        }
    }

    private void PrintWelcome()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("MyCancerTeam interactive session started.");
            Console.WriteLine("Type a short update/question and press Enter, or use a command:");
            Console.WriteLine("  draft        - create a communication draft");
            Console.WriteLine("  help         - show available commands");
            Console.WriteLine("  exit / quit  - stop the session (Ctrl+C also works)");
            Console.WriteLine("New note files dropped into the watched folders are processed automatically.");
        }
    }

    private void PrintHelp()
    {
        lock (_consoleLock)
        {
            Console.WriteLine("Commands:");
            Console.WriteLine("  <text>       - send an update/question to the team lead");
            Console.WriteLine("  draft        - create a communication draft");
            Console.WriteLine("  help         - show this help");
            Console.WriteLine("  exit / quit  - stop the session");
            Console.WriteLine("Watched folders:");
            foreach (var folder in _scanner.WatchedFolders)
            {
                Console.WriteLine($"  - {folder}");
            }
        }
    }

    private enum WorkItemKind
    {
        UserQuery,
        FileNote
    }

    private sealed record WorkItem(WorkItemKind Kind, string Input, string Source);
}
