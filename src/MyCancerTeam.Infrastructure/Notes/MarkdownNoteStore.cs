using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Notes;

namespace MyCancerTeam.Infrastructure.Notes;

public sealed class MarkdownNoteStore : INoteStore
{
    private readonly AppConfiguration _configuration;

    public MarkdownNoteStore(AppConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<string> ReadSharedNotesAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configuration.SharedNotesFilePath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(_configuration.SharedNotesFilePath, cancellationToken);
    }

    public async Task WriteSharedNotesAsync(string content, CancellationToken cancellationToken = default)
    {
        var folder = Path.GetDirectoryName(_configuration.SharedNotesFilePath) ?? _configuration.OurNotesFolderPath;
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(_configuration.SharedNotesFilePath, content, cancellationToken);
    }

    public async Task WriteAgentNotesAsync(string agentFileName, string content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_configuration.OurNotesFolderPath);

        var safeFileName = Path.GetFileName(agentFileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName is "." or "..")
        {
            throw new ArgumentException("Agent notes file name must be a simple file name.", nameof(agentFileName));
        }

        var path = Path.Combine(_configuration.OurNotesFolderPath, safeFileName);
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }
}
