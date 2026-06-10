namespace MyCancerTeam.Core.Research;

public interface IResearchOncologyService
{
    Task<ResearchUpdate> GetLatestEvidenceAsync(string patientContext, CancellationToken cancellationToken = default);
}
