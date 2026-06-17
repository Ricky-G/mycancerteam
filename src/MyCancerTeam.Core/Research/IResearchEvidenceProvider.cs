namespace MyCancerTeam.Core.Research;

public interface IResearchEvidenceProvider
{
    Task<ResearchEvidenceSnapshot> GetEvidenceAsync(string patientContext, CancellationToken cancellationToken = default);
}
