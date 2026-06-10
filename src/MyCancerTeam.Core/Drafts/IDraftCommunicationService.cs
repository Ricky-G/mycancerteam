namespace MyCancerTeam.Core.Drafts;

public interface IDraftCommunicationService
{
    Task<DraftCommunicationResult> CreateDraftAsync(DraftCommunicationRequest request, CancellationToken cancellationToken = default);
}
