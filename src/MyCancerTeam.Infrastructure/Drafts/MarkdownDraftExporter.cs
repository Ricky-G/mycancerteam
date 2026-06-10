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

        var folder = Path.Combine(_configuration.DraftCommunicationsFolderPath, safeType);
        Directory.CreateDirectory(folder);

        var fileName = $"{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}-draft.md";
        var path = Path.Combine(folder, fileName);

        await File.WriteAllTextAsync(path, markdownContent, cancellationToken);
        return path;
    }
}
