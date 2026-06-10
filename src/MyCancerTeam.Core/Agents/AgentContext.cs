using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Core.Agents;

public sealed class AgentContext
{
    public required WorkflowRequest WorkflowRequest { get; init; }
    public string SharedNotes { get; init; } = string.Empty;
    public DateTimeOffset TimestampUtc { get; init; } = DateTimeOffset.UtcNow;
}
