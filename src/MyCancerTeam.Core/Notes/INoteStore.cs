namespace MyCancerTeam.Core.Notes;

public interface INoteStore
{
    Task<string> ReadSharedNotesAsync(CancellationToken cancellationToken = default);
    Task WriteSharedNotesAsync(string content, CancellationToken cancellationToken = default);
    Task WriteAgentNotesAsync(string agentFileName, string content, CancellationToken cancellationToken = default);
}
