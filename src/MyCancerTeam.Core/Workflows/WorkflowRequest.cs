namespace MyCancerTeam.Core.Workflows;

public sealed class WorkflowRequest
{
    public required WorkflowType WorkflowType { get; init; }
    public required string UserInput { get; init; }
}
