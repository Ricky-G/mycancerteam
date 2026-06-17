using System.Text;
using System.Text.RegularExpressions;
using MyCancerTeam.Core.AI;
using MyCancerTeam.Core.Agents;

namespace MyCancerTeam.Infrastructure.Notes;

public sealed class TeamLeadSummaryComposer
{
    private const string LlmSystemPrompt = """
        You format a multi-disciplinary team (MDT) summary for a cancer patient and their support people.

        You will receive a structured snapshot with sections: Current Diagnosis, Current Treatment, Next Steps, and Engaged Agents.
        Re-render it as a clean, easy-to-scan Markdown document. Rules:
        - Preserve every distinct fact; do not invent or omit information.
        - Use the exact headings: "# MyCancerTeam Summary", "## Current Diagnosis", "## Current Treatment", "## Next Steps", "## Engaged Agents".
        - Format each section's content as concise bullet points (use sub-bullets where needed to group related details). Use short paragraphs only when a bullet-point list would feel unnatural.
        - NEVER attribute statements to individual specialists or roles (no "PatientOwner:", "Radiologist:", "Research Oncology:" style prefixes). The team speaks with a single voice.
        - Next Steps must be a bulleted list of actionable items or outstanding questions.
        - Engaged Agents must be a single comma-separated line (this is the ONLY place roles are named).
        - Keep medical caveats and uncertainties intact.
        - Protect patient privacy: never include the patient's name or other personal identifiers. Refer to the patient only as "the patient", and replace any name that appears in the input with "the patient".
        - Output Markdown only; no preamble, no code fences.
        """;

    private readonly ILlmChatClient? _llmClient;

    public TeamLeadSummaryComposer(ILlmChatClient? llmClient = null)
    {
        _llmClient = llmClient;
    }

    public async Task<string> ComposeAsync(string previousSummary, string source, string input, AgentResponse response, CancellationToken cancellationToken = default)
    {
        var snapshot = BuildSnapshot(previousSummary, response);
        var deterministic = RenderDeterministic(snapshot);

        if (_llmClient is null)
        {
            return deterministic;
        }

        try
        {
            var formatted = await _llmClient.CompleteTextAsync(LlmSystemPrompt, deterministic, cancellationToken);
            if (string.IsNullOrWhiteSpace(formatted))
            {
                return deterministic;
            }

            var stamped = StampLastUpdated(formatted.Trim());
            return stamped;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return deterministic;
        }
    }

    public static string Compose(string previousSummary, string source, string input, AgentResponse response)
    {
        var snapshot = BuildSnapshot(previousSummary, response);
        return RenderDeterministic(snapshot);
    }

    // The latest synthesized response is the authoritative current state (the agent already
    // folded the previous summary into it). The previous summary is only consulted as a fallback
    // for sections the response left empty, so we never regress an already-known fact.
    private static SummarySnapshot BuildSnapshot(string previousSummary, AgentResponse response)
    {
        var current = CreateSnapshotFromResponse(response);
        var prior = ParseSnapshot(previousSummary);

        if (prior is null)
        {
            return current;
        }

        var diagnosis = IsUseful(current.CurrentDiagnosis) ? current.CurrentDiagnosis : prior.CurrentDiagnosis;
        var treatment = IsUseful(current.CurrentTreatment) ? current.CurrentTreatment : prior.CurrentTreatment;
        var nextSteps = current.NextSteps.Count > 0 && current.NextSteps.Any(IsUseful)
            ? current.NextSteps
            : prior.NextSteps;

        var engagedAgents = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var agent in current.EngagedAgents.Concat(prior.EngagedAgents))
        {
            if (seen.Add(agent))
            {
                engagedAgents.Add(agent);
            }
        }

        return new SummarySnapshot(diagnosis, treatment, nextSteps, engagedAgents);
    }

    private static string RenderDeterministic(SummarySnapshot snapshot)
    {
        var summary = new StringBuilder();
        summary.AppendLine("# MyCancerTeam Summary");
        summary.AppendLine();
        summary.AppendLine($"_Last updated: {DateTimeOffset.UtcNow:O}_");
        summary.AppendLine();
        summary.AppendLine("## Current Diagnosis");
        summary.AppendLine(snapshot.CurrentDiagnosis);
        summary.AppendLine();
        summary.AppendLine("## Current Treatment");
        summary.AppendLine(snapshot.CurrentTreatment);
        summary.AppendLine();
        summary.AppendLine("## Next Steps");
        foreach (var nextStep in snapshot.NextSteps)
        {
            summary.AppendLine($"- {nextStep}");
        }
        summary.AppendLine();
        summary.AppendLine("## Engaged Agents");
        summary.AppendLine(snapshot.EngagedAgents.Count == 0 ? "Team Lead" : string.Join(", ", snapshot.EngagedAgents));

        return summary.ToString();
    }

    private static string StampLastUpdated(string formatted)
    {
        var stamp = $"_Last updated: {DateTimeOffset.UtcNow:O}_";

        if (formatted.Contains("_Last updated:", StringComparison.OrdinalIgnoreCase))
        {
            return Regex.Replace(formatted, @"_Last updated:[^_\r\n]*_", stamp);
        }

        var headingMatch = Regex.Match(formatted, @"^# .*\r?\n", RegexOptions.Multiline);
        if (headingMatch.Success)
        {
            return formatted.Insert(headingMatch.Index + headingMatch.Length, Environment.NewLine + stamp + Environment.NewLine);
        }

        return stamp + Environment.NewLine + Environment.NewLine + formatted;
    }

    private static SummarySnapshot? ParseSnapshot(string previousSummary)
    {
        if (string.IsNullOrWhiteSpace(previousSummary))
        {
            return null;
        }

        // The previous summary is the rendered summary.md document with level-2 sections.
        var currentDiagnosis = ExtractSectionText(previousSummary, "Current Diagnosis") ?? string.Empty;
        var currentTreatment = ExtractSectionText(previousSummary, "Current Treatment") ?? string.Empty;

        var nextSteps = ExtractSectionLines(previousSummary, "Next Steps")
            .Where(IsUseful)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        var engagedAgents = new List<string>();
        var seenAgents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var agent in ExtractSectionLines(previousSummary, "Engaged Agents")
                     .SelectMany(line => line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)))
        {
            if (seenAgents.Add(agent))
            {
                engagedAgents.Add(agent);
            }
        }

        if (string.IsNullOrWhiteSpace(currentDiagnosis)
            && string.IsNullOrWhiteSpace(currentTreatment)
            && nextSteps.Count == 0
            && engagedAgents.Count == 0)
        {
            return null;
        }

        return new SummarySnapshot(
            string.IsNullOrWhiteSpace(currentDiagnosis) ? "Not yet established." : TrimBrief(currentDiagnosis),
            string.IsNullOrWhiteSpace(currentTreatment) ? "Not yet established." : TrimBrief(currentTreatment),
            nextSteps.Count == 0 ? ["Continue clinician follow-up."] : nextSteps,
            engagedAgents);
    }

    private static SummarySnapshot CreateSnapshotFromResponse(AgentResponse response)
    {
        var nextSteps = response.SuggestedClinicianQuestions
            .Concat(response.OpenQuestions)
            .Where(item => !string.IsNullOrWhiteSpace(item))
            .Distinct()
            .Take(5)
            .ToList();

        if (nextSteps.Count == 0)
        {
            nextSteps = ["Continue clinician follow-up."];
        }

        return new SummarySnapshot(
            TrimBrief(response.Summary),
            TrimBrief(string.IsNullOrWhiteSpace(response.TechnicalSummary) ? "Not yet established." : response.TechnicalSummary),
            nextSteps,
            response.EngagedAgents.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string TrimBrief(string value)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
        {
            return "Not yet established.";
        }

        var lines = trimmed
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(line => line.Trim())
            .Where(line => line.Length > 0 && IsUseful(line.StartsWith("- ") ? line[2..] : line))
            .Take(6)
            .ToList();

        if (lines.Count > 1)
        {
            return string.Join(Environment.NewLine, lines);
        }

        var single = lines.Count == 1 ? lines[0] : trimmed;
        return single.Length > 1200 ? single[..1200].TrimEnd() + "..." : single;
    }

    private static string? ExtractSectionText(string block, string heading)
    {
        var lines = ExtractSectionLines(block, heading).ToList();
        if (lines.Count == 0)
        {
            return null;
        }

        return string.Join(Environment.NewLine, lines).Trim();
    }

    private static IEnumerable<string> ExtractSectionLines(string block, string heading)
    {
        var pattern = $@"(?ms)^#{{2,3}} {Regex.Escape(heading)}\s*(?<content>.*?)(?=^#{{2,3}} |\z)";
        var match = Regex.Match(block, pattern);
        if (!match.Success)
        {
            return [];
        }

        var content = match.Groups["content"].Value;
        var lines = content.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return lines
            .Select(line => line.StartsWith("- ") ? line[2..].Trim() : line.Trim())
            .Where(line => line.Length > 0 && !line.StartsWith("_Last updated:", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsUseful(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var trimmed = value.Trim();
        return !trimmed.StartsWith("Team synthesis prepared", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("Consolidated specialist viewpoints", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Equals("Not yet established.", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Equals("Awaiting specialist input.", StringComparison.OrdinalIgnoreCase)
            && !trimmed.Equals("No specialist treatment notes yet.", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("Response could not be parsed", StringComparison.OrdinalIgnoreCase)
            && !trimmed.StartsWith("Please retry", StringComparison.OrdinalIgnoreCase);
    }

    private sealed record SummarySnapshot(
        string CurrentDiagnosis,
        string CurrentTreatment,
        IReadOnlyList<string> NextSteps,
        IReadOnlyList<string> EngagedAgents);
}
