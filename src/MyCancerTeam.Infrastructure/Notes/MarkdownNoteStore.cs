using System.Text.Json;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Notes;

namespace MyCancerTeam.Infrastructure.Notes;

public sealed class MarkdownNoteStore : INoteStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

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

    public async Task<string> ReadSummaryAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configuration.SummaryFilePath))
        {
            return string.Empty;
        }

        return await File.ReadAllTextAsync(_configuration.SummaryFilePath, cancellationToken);
    }

    public async Task WriteSummaryAsync(string content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_configuration.LocalWorkingFolderPath);
        await File.WriteAllTextAsync(_configuration.SummaryFilePath, content, cancellationToken);
    }

    public async Task<MdtState?> ReadMdtStateAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_configuration.MdtStateFilePath))
        {
            return null;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_configuration.MdtStateFilePath, cancellationToken);
            return JsonSerializer.Deserialize<MdtState>(json, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public async Task WriteMdtStateAsync(MdtState state, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_configuration.LocalWorkingFolderPath);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        await File.WriteAllTextAsync(_configuration.MdtStateFilePath, json, cancellationToken);
    }
}
