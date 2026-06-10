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

        var markdown = $"""
# Research Update

- Generated (UTC): {generatedUtc:O}
- Schedule setting: {_configuration.DailyResearchRefreshSchedule ?? "not set"}

## Patient-Friendly Summary
{update.PatientFriendlySummary}

## Technical Summary
{update.TechnicalSummary}
""";

        await File.WriteAllTextAsync(filePath, markdown, cancellationToken);
    }
}
