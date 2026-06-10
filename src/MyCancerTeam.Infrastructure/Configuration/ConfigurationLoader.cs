using System.Text.Json;
using MyCancerTeam.Core.Configuration;

namespace MyCancerTeam.Infrastructure.Configuration;

public sealed class ConfigurationLoader
{
    public AppConfiguration Load(string repositoryRootPath)
    {
        var environment = Environment.GetEnvironmentVariable("MYCANCERTEAM_ENVIRONMENT") ?? "dev";
        var configPath = Path.Combine(repositoryRootPath, "config", "environments", environment, "appsettings.json");

        AppConfiguration configuration = new()
        {
            EnvironmentName = environment
        };

        if (File.Exists(configPath))
        {
            var json = File.ReadAllText(configPath);
            var parsed = JsonSerializer.Deserialize<AppConfiguration>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (parsed is not null)
            {
                configuration = parsed;
                configuration.EnvironmentName = environment;
            }
        }

        ApplyEnvironmentOverrides(configuration);
        ApplyPathDefaults(configuration, repositoryRootPath);

        return configuration;
    }

    public void EnsureLocalDirectories(AppConfiguration configuration)
    {
        foreach (var folder in GetAllLocalDirectories(configuration))
        {
            Directory.CreateDirectory(folder);
        }
    }

    private static IReadOnlyList<string> GetAllLocalDirectories(AppConfiguration configuration) =>
    [
        configuration.LocalWorkingFolderPath,
        configuration.IterationsFolderPath,
        configuration.ClinicalNotesFolderPath,
        configuration.ReportsFolderPath,
        configuration.ImagingFolderPath,
        configuration.RadiationPlanFolderPath,
        configuration.MedicationPlanFolderPath,
        configuration.InsuranceDocumentsFolderPath,
        configuration.ResearchCacheFolderPath,
        configuration.ResearchSummariesFolderPath,
        configuration.GlobalTreatmentSearchCacheFolderPath,
        configuration.InternationalSecondOpinionDocumentsFolderPath,
        configuration.DraftCommunicationsFolderPath,
        configuration.AgentMemoryFolderPath,
        Path.GetDirectoryName(configuration.LatestSharedNotesPath) ?? configuration.LocalWorkingFolderPath,
        Path.Combine(configuration.DraftCommunicationsFolderPath, "emails"),
        Path.Combine(configuration.DraftCommunicationsFolderPath, "insurance"),
        Path.Combine(configuration.DraftCommunicationsFolderPath, "second-opinions"),
        Path.Combine(configuration.DraftCommunicationsFolderPath, "trials")
    ];

    private static void ApplyEnvironmentOverrides(AppConfiguration configuration)
    {
        configuration.AzureOpenAiEndpoint = Get("MYCANCERTEAM_AZURE_OPENAI_ENDPOINT", configuration.AzureOpenAiEndpoint);
        configuration.AzureOpenAiDeployment = Get("MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT", configuration.AzureOpenAiDeployment);
        configuration.LocalWorkingFolderPath = Get("MYCANCERTEAM_LOCAL_WORKING_FOLDER", configuration.LocalWorkingFolderPath);
        configuration.IterationsFolderPath = Get("MYCANCERTEAM_ITERATIONS_FOLDER", configuration.IterationsFolderPath);
        configuration.ClinicalNotesFolderPath = Get("MYCANCERTEAM_CLINICAL_NOTES_FOLDER", configuration.ClinicalNotesFolderPath);
        configuration.ReportsFolderPath = Get("MYCANCERTEAM_REPORTS_FOLDER", configuration.ReportsFolderPath);
        configuration.ImagingFolderPath = Get("MYCANCERTEAM_IMAGING_FOLDER", configuration.ImagingFolderPath);
        configuration.RadiationPlanFolderPath = Get("MYCANCERTEAM_RADIATION_PLAN_FOLDER", configuration.RadiationPlanFolderPath);
        configuration.MedicationPlanFolderPath = Get("MYCANCERTEAM_MEDICATION_PLAN_FOLDER", configuration.MedicationPlanFolderPath);
        configuration.InsuranceDocumentsFolderPath = Get("MYCANCERTEAM_INSURANCE_DOCUMENTS_FOLDER", configuration.InsuranceDocumentsFolderPath);
        configuration.ResearchCacheFolderPath = Get("MYCANCERTEAM_RESEARCH_CACHE_FOLDER", configuration.ResearchCacheFolderPath);
        configuration.ResearchSummariesFolderPath = Get("MYCANCERTEAM_RESEARCH_SUMMARIES_FOLDER", configuration.ResearchSummariesFolderPath);
        configuration.GlobalTreatmentSearchCacheFolderPath = Get("MYCANCERTEAM_GLOBAL_TREATMENT_SEARCH_FOLDER", configuration.GlobalTreatmentSearchCacheFolderPath);
        configuration.InternationalSecondOpinionDocumentsFolderPath = Get("MYCANCERTEAM_INTL_SECOND_OPINIONS_FOLDER", configuration.InternationalSecondOpinionDocumentsFolderPath);
        configuration.DraftCommunicationsFolderPath = Get("MYCANCERTEAM_DRAFTS_FOLDER", configuration.DraftCommunicationsFolderPath);
        configuration.AgentMemoryFolderPath = Get("MYCANCERTEAM_AGENT_MEMORY_FOLDER", configuration.AgentMemoryFolderPath);
        configuration.LatestSharedNotesPath = Get("MYCANCERTEAM_LATEST_SHARED_NOTES_PATH", configuration.LatestSharedNotesPath);
        configuration.DailyResearchRefreshSchedule = GetNullable("MYCANCERTEAM_DAILY_RESEARCH_REFRESH_SCHEDULE", configuration.DailyResearchRefreshSchedule);

        static string Get(string key, string fallback)
            => Environment.GetEnvironmentVariable(key) ?? fallback;

        static string? GetNullable(string key, string? fallback)
            => Environment.GetEnvironmentVariable(key) ?? fallback;
    }

    private static void ApplyPathDefaults(AppConfiguration configuration, string rootPath)
    {
        var localRoot = ToAbsolute(configuration.LocalWorkingFolderPath, rootPath, ".local");
        configuration.LocalWorkingFolderPath = localRoot;
        configuration.IterationsFolderPath = ToAbsolute(configuration.IterationsFolderPath, rootPath, Path.Combine(localRoot, "iterations"));
        configuration.ClinicalNotesFolderPath = ToAbsolute(configuration.ClinicalNotesFolderPath, rootPath, Path.Combine(localRoot, "clinical-notes"));
        configuration.ReportsFolderPath = ToAbsolute(configuration.ReportsFolderPath, rootPath, Path.Combine(localRoot, "reports"));
        configuration.ImagingFolderPath = ToAbsolute(configuration.ImagingFolderPath, rootPath, Path.Combine(localRoot, "imaging"));
        configuration.RadiationPlanFolderPath = ToAbsolute(configuration.RadiationPlanFolderPath, rootPath, Path.Combine(localRoot, "radiation-plans"));
        configuration.MedicationPlanFolderPath = ToAbsolute(configuration.MedicationPlanFolderPath, rootPath, Path.Combine(localRoot, "medication-plans"));
        configuration.InsuranceDocumentsFolderPath = ToAbsolute(configuration.InsuranceDocumentsFolderPath, rootPath, Path.Combine(localRoot, "insurance-documents"));
        configuration.ResearchCacheFolderPath = ToAbsolute(configuration.ResearchCacheFolderPath, rootPath, Path.Combine(localRoot, "research-cache"));
        configuration.ResearchSummariesFolderPath = ToAbsolute(configuration.ResearchSummariesFolderPath, rootPath, Path.Combine(localRoot, "research-summaries"));
        configuration.GlobalTreatmentSearchCacheFolderPath = ToAbsolute(configuration.GlobalTreatmentSearchCacheFolderPath, rootPath, Path.Combine(localRoot, "global-treatment-search"));
        configuration.InternationalSecondOpinionDocumentsFolderPath = ToAbsolute(configuration.InternationalSecondOpinionDocumentsFolderPath, rootPath, Path.Combine(localRoot, "international-second-opinions"));
        configuration.DraftCommunicationsFolderPath = ToAbsolute(configuration.DraftCommunicationsFolderPath, rootPath, Path.Combine(localRoot, "drafts"));
        configuration.AgentMemoryFolderPath = ToAbsolute(configuration.AgentMemoryFolderPath, rootPath, Path.Combine(localRoot, "agent-memory"));
        configuration.LatestSharedNotesPath = ToAbsolute(configuration.LatestSharedNotesPath, rootPath, Path.Combine(localRoot, "notes", "notes.md"));
    }

    private static string ToAbsolute(string value, string rootPath, string fallback)
    {
        var selected = string.IsNullOrWhiteSpace(value) ? fallback : value;

        if (Path.IsPathRooted(selected))
        {
            return selected;
        }

        return Path.GetFullPath(Path.Combine(rootPath, selected));
    }
}
