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
        configuration.MedicalNotesFolderPath,
        configuration.NonMedicalNotesFolderPath,
        configuration.ResearchFolderPath,
        configuration.OurNotesFolderPath
    ];

    private static void ApplyEnvironmentOverrides(AppConfiguration configuration)
    {
        configuration.AzureOpenAiEndpoint = Get("MYCANCERTEAM_AZURE_OPENAI_ENDPOINT", configuration.AzureOpenAiEndpoint);
        configuration.AzureOpenAiDeployment = Get("MYCANCERTEAM_AZURE_OPENAI_DEPLOYMENT", configuration.AzureOpenAiDeployment);
        configuration.LocalWorkingFolderPath = Get("MYCANCERTEAM_LOCAL_WORKING_FOLDER", configuration.LocalWorkingFolderPath);
        configuration.MedicalNotesFolderPath = Get("MYCANCERTEAM_MEDICAL_NOTES_FOLDER", configuration.MedicalNotesFolderPath);
        configuration.NonMedicalNotesFolderPath = Get("MYCANCERTEAM_NON_MEDICAL_NOTES_FOLDER", configuration.NonMedicalNotesFolderPath);
        configuration.ResearchFolderPath = Get("MYCANCERTEAM_RESEARCH_FOLDER", configuration.ResearchFolderPath);
        configuration.OurNotesFolderPath = Get("MYCANCERTEAM_OUR_NOTES_FOLDER", configuration.OurNotesFolderPath);
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
        configuration.MedicalNotesFolderPath = ToAbsolute(configuration.MedicalNotesFolderPath, rootPath, Path.Combine(localRoot, "medical-notes"));
        configuration.NonMedicalNotesFolderPath = ToAbsolute(configuration.NonMedicalNotesFolderPath, rootPath, Path.Combine(localRoot, "non-medical-notes"));
        configuration.ResearchFolderPath = ToAbsolute(configuration.ResearchFolderPath, rootPath, Path.Combine(localRoot, "research"));
        configuration.OurNotesFolderPath = ToAbsolute(configuration.OurNotesFolderPath, rootPath, Path.Combine(localRoot, "our-notes"));
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
