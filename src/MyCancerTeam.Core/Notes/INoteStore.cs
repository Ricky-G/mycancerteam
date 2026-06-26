using MyCancerTeam.Core.Agents;

namespace MyCancerTeam.Core.Notes;

public interface INoteStore
{
    Task<string> ReadSharedNotesAsync(CancellationToken cancellationToken = default);
    Task WriteSharedNotesAsync(string content, CancellationToken cancellationToken = default);
    Task WriteAgentNotesAsync(string agentFileName, string content, CancellationToken cancellationToken = default);
    Task<string> ReadSummaryAsync(CancellationToken cancellationToken = default);
    Task WriteSummaryAsync(string content, CancellationToken cancellationToken = default);
    Task<MdtState?> ReadMdtStateAsync(CancellationToken cancellationToken = default);
    Task WriteMdtStateAsync(MdtState state, CancellationToken cancellationToken = default);
}
