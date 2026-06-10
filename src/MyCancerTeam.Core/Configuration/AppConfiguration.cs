namespace MyCancerTeam.Core.Configuration;

public sealed class AppConfiguration
{
    public string EnvironmentName { get; set; } = "dev";
    public string AzureOpenAiEndpoint { get; set; } = string.Empty;
    public string AzureOpenAiDeployment { get; set; } = string.Empty;
    public string LocalWorkingFolderPath { get; set; } = string.Empty;
    public string IterationsFolderPath { get; set; } = string.Empty;
    public string ClinicalNotesFolderPath { get; set; } = string.Empty;
    public string ReportsFolderPath { get; set; } = string.Empty;
    public string ImagingFolderPath { get; set; } = string.Empty;
    public string RadiationPlanFolderPath { get; set; } = string.Empty;
    public string MedicationPlanFolderPath { get; set; } = string.Empty;
    public string InsuranceDocumentsFolderPath { get; set; } = string.Empty;
    public string ResearchCacheFolderPath { get; set; } = string.Empty;
    public string ResearchSummariesFolderPath { get; set; } = string.Empty;
    public string GlobalTreatmentSearchCacheFolderPath { get; set; } = string.Empty;
    public string InternationalSecondOpinionDocumentsFolderPath { get; set; } = string.Empty;
    public string DraftCommunicationsFolderPath { get; set; } = string.Empty;
    public string AgentMemoryFolderPath { get; set; } = string.Empty;
    public string LatestSharedNotesPath { get; set; } = string.Empty;
    public string? DailyResearchRefreshSchedule { get; set; }
}
