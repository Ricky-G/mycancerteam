using System.Text;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public sealed class ResearchRefreshService : IResearchRefreshService
{
    private readonly IResearchOncologyService _researchOncologyService;
    private readonly AppConfiguration _configuration;

    public ResearchRefreshService(IResearchOncologyService researchOncologyService, AppConfiguration configuration)
    {
        _researchOncologyService = researchOncologyService;
        _configuration = configuration;
    }

    public async Task RefreshAsync(string patientContext, CancellationToken cancellationToken = default)
    {
        var update = await _researchOncologyService.GetLatestEvidenceAsync(patientContext, cancellationToken);
        Directory.CreateDirectory(_configuration.ResearchFolderPath);

        var generatedUtc = DateTimeOffset.UtcNow;
        var filePath = Path.Combine(_configuration.ResearchFolderPath, $"{generatedUtc:yyyyMMdd-HHmmss}-research-update.md");

        var markdown = new StringBuilder()
            .AppendLine("# Research Update")
            .AppendLine()
            .AppendLine($"- Generated (UTC): {generatedUtc:O}")
            .AppendLine($"- Schedule setting: {_configuration.DailyResearchRefreshSchedule ?? "not set"}")
            .AppendLine()
            .AppendLine("## Patient-Friendly Summary")
            .AppendLine(update.PatientFriendlySummary)
            .AppendLine()
            .AppendLine("## Technical Summary")
            .AppendLine(update.TechnicalSummary);

        if (update.EvidenceGapNotes.Count > 0)
        {
            markdown.AppendLine()
                .AppendLine("## Evidence Gaps")
                .AppendLine();

            foreach (var note in update.EvidenceGapNotes)
            {
                markdown.AppendLine($"- {note}");
            }
        }

        if (update.Citations.Count > 0)
        {
            markdown.AppendLine()
                .AppendLine("## Citations")
                .AppendLine();

            foreach (var citation in update.Citations)
            {
                markdown.AppendLine($"- [{citation.Title}]({citation.Url}) — {citation.SourceName} ({citation.EvidenceLevel})");
            }
        }

        var fileContent = markdown.ToString();

        await File.WriteAllTextAsync(filePath, fileContent, cancellationToken);
    }
}
