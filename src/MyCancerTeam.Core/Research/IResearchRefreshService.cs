namespace MyCancerTeam.Core.Research;

public interface IResearchRefreshService
{
    Task RefreshAsync(string patientContext, CancellationToken cancellationToken = default);
}
