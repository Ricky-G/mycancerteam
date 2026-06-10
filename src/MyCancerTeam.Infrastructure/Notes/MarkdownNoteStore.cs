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
        if (!File.Exists(_configuration.LatestSharedNotesPath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(_configuration.LatestSharedNotesPath, cancellationToken);
    }

    public async Task WriteSharedNotesAsync(string content, CancellationToken cancellationToken = default)
    {
        var folder = Path.GetDirectoryName(_configuration.LatestSharedNotesPath) ?? _configuration.LocalWorkingFolderPath;
        Directory.CreateDirectory(folder);
        await File.WriteAllTextAsync(_configuration.LatestSharedNotesPath, content, cancellationToken);
    }

    public async Task WriteAgentNotesAsync(string agentFileName, string content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_configuration.AgentMemoryFolderPath);

        var safeFileName = Path.GetFileName(agentFileName);
        if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName is "." or "..")
        {
            throw new ArgumentException("Agent notes file name must be a simple file name.", nameof(agentFileName));
        }

        var path = Path.Combine(_configuration.AgentMemoryFolderPath, safeFileName);
        await File.WriteAllTextAsync(path, content, cancellationToken);
    }
}
