namespace MyCancerTeam.Core.Models;

public sealed class InternationalSecondOpinionRequest
{
    public required string TargetInstitution { get; init; }
    public string MissingInformationChecklist { get; init; } = string.Empty;
}
