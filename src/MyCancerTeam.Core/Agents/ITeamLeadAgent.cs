using MyCancerTeam.Core.Workflows;

namespace MyCancerTeam.Core.Agents;

public interface ITeamLeadAgent : IAgent
{
    Task<AgentResponse> CoordinateAsync(WorkflowRequest request, string sharedNotes, CancellationToken cancellationToken = default);
}
