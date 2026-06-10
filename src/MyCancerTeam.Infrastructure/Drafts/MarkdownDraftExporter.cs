using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Drafts;

namespace MyCancerTeam.Infrastructure.Drafts;

public sealed class MarkdownDraftExporter : IMarkdownDraftExporter
{
    private readonly AppConfiguration _configuration;

    public MarkdownDraftExporter(AppConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> ExportAsync(string draftType, string markdownContent, CancellationToken cancellationToken = default)
    {
        var safeType = string.IsNullOrWhiteSpace(draftType)
            ? "general"
            : new string(draftType
                .Trim()
                .ToLowerInvariant()
                .Select(c => char.IsLetterOrDigit(c) || c is '-' or '_' ? c : '-')
                .ToArray())
                .Trim('-');

        safeType = Path.GetFileName(safeType);
        if (string.IsNullOrWhiteSpace(safeType) || safeType is "." or "..")
        {
            safeType = "general";
        }

        Directory.CreateDirectory(_configuration.OurNotesFolderPath);

        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-{safeType}-draft.md";
        var path = Path.Combine(_configuration.OurNotesFolderPath, fileName);

        await File.WriteAllTextAsync(path, markdownContent, cancellationToken);
        return path;
    }
}
