namespace MyCancerTeam.Core.Configuration;

public sealed class AppConfiguration
{
    public string EnvironmentName { get; set; } = "dev";
    public string AzureOpenAiEndpoint { get; set; } = string.Empty;
    public string AzureOpenAiDeployment { get; set; } = string.Empty;
    public string LocalWorkingFolderPath { get; set; } = string.Empty;

    /// <summary>Medical input notes: clinical notes, reports, imaging, radiation and medication plans.</summary>
    public string MedicalNotesFolderPath { get; set; } = string.Empty;

    /// <summary>Non-medical input notes: insurance documents and international second-opinion paperwork.</summary>
    public string NonMedicalNotesFolderPath { get; set; } = string.Empty;

    /// <summary>Research input/output: cached searches, evidence summaries and global treatment searches.</summary>
    public string ResearchFolderPath { get; set; } = string.Empty;

    /// <summary>The application's own output: shared notes, agent memory and drafts.</summary>
    public string OurNotesFolderPath { get; set; } = string.Empty;

    public string? DailyResearchRefreshSchedule { get; set; }

    /// <summary>The shared notes markdown file, stored within <see cref="OurNotesFolderPath"/>.</summary>
    public string SharedNotesFilePath => Path.Combine(OurNotesFolderPath, "notes.md");

    /// <summary>The summary markdown file, stored at the <see cref="LocalWorkingFolderPath"/> root.</summary>
    public string SummaryFilePath => Path.Combine(LocalWorkingFolderPath, "summary.md");
}
