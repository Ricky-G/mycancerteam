namespace MyCancerTeam.Core.Models;

public sealed class InsuranceDocumentChecklist
{
    public IReadOnlyList<string> RequiredDocuments { get; init; } = [];
}
