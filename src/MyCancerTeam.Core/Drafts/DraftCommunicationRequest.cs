namespace MyCancerTeam.Core.Drafts;

public sealed class DraftCommunicationRequest
{
    public required string DraftType { get; init; }
    public required string RecipientType { get; init; }
    public required string Subject { get; init; }
    public required string PatientContextSummary { get; init; }
    public string AdditionalDetails { get; init; } = string.Empty;
}
