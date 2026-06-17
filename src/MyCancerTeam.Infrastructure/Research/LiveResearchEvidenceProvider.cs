using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyCancerTeam.Core.Models;
using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public sealed class LiveResearchEvidenceProvider : IResearchEvidenceProvider
{
    private const int MaxSourceCount = 3;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly IResearchWebSearchProvider _webSearchProvider;
    private readonly HttpClient _httpClient;

    public LiveResearchEvidenceProvider(IResearchWebSearchProvider webSearchProvider, HttpClient httpClient)
    {
        _webSearchProvider = webSearchProvider;
        _httpClient = httpClient;
    }

    public async Task<ResearchEvidenceSnapshot> GetEvidenceAsync(string patientContext, CancellationToken cancellationToken = default)
    {
        var snapshot = await _webSearchProvider.SearchAsync(patientContext, cancellationToken);
        var sources = snapshot.Sources.ToList();
        var warnings = snapshot.Warnings.ToList();

        sources.AddRange(await GetPubMedSourcesAsync(patientContext, warnings, cancellationToken));
        sources.AddRange(await GetClinicalTrialSourcesAsync(patientContext, warnings, cancellationToken));

        return new ResearchEvidenceSnapshot
        {
            Sources = sources
                .GroupBy(source => source.Citation.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray(),
            Warnings = warnings.Distinct(StringComparer.OrdinalIgnoreCase).ToArray()
        };
    }

    private async Task<IReadOnlyList<ResearchEvidenceSource>> GetPubMedSourcesAsync(
        string patientContext,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<PubMedSearchResponse>(
                BuildPubMedUri(patientContext),
                JsonOptions,
                cancellationToken);

            var ids = response?.SearchResult?.Ids?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Take(MaxSourceCount)
                .ToArray() ?? [];

            if (ids.Length == 0)
            {
                warnings.Add("PubMed returned no matching studies.");
                return [];
            }

            return ids.Select(id => new ResearchEvidenceSource
            {
                Citation = new CitationMetadata
                {
                    SourceName = "PubMed",
                    Title = $"PubMed article {id}",
                    Url = $"https://pubmed.ncbi.nlm.nih.gov/{id}/",
                    EvidenceLevel = "publication"
                },
                Summary = "PubMed article identified for follow-up review."
            }).ToArray();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            warnings.Add($"PubMed lookup failed: {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<ResearchEvidenceSource>> GetClinicalTrialSourcesAsync(
        string patientContext,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            var response = await _httpClient.GetFromJsonAsync<ClinicalTrialsResponse>(
                BuildClinicalTrialsUri(patientContext),
                JsonOptions,
                cancellationToken);

            var studies = response?.StudyFieldsResponse?.StudyFields?
                .Where(study => study is not null)
                .Take(MaxSourceCount)
                .ToArray() ?? [];

            if (studies.Length == 0)
            {
                warnings.Add("ClinicalTrials.gov returned no matching studies.");
                return [];
            }

            return studies
                .Select(CreateClinicalTrialSource)
                .Where(source => source is not null)
                .Cast<ResearchEvidenceSource>()
                .ToArray();
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            warnings.Add($"ClinicalTrials.gov lookup failed: {ex.Message}");
            return [];
        }
    }

    private static ResearchEvidenceSource? CreateClinicalTrialSource(StudyFields study)
    {
        var trialId = study.NctId?.FirstOrDefault();
        if (string.IsNullOrWhiteSpace(trialId))
        {
            return null;
        }

        var title = study.BriefTitle?.FirstOrDefault();
        var condition = study.Condition?.FirstOrDefault();

        return new ResearchEvidenceSource
        {
            Citation = new CitationMetadata
            {
                SourceName = "ClinicalTrials.gov",
                Title = string.IsNullOrWhiteSpace(title) ? trialId : title,
                Url = $"https://clinicaltrials.gov/study/{trialId}",
                EvidenceLevel = "clinical trial"
            },
            Summary = string.IsNullOrWhiteSpace(condition)
                ? "Clinical trial identified for follow-up review."
                : $"Clinical trial related to {condition}."
        };
    }

    private static string BuildPubMedUri(string patientContext)
        => $"https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi?db=pubmed&retmode=json&retmax={MaxSourceCount}&term={Uri.EscapeDataString(patientContext)}";

    private static string BuildClinicalTrialsUri(string patientContext)
        => $"https://clinicaltrials.gov/api/query/study_fields?expr={Uri.EscapeDataString(patientContext)}&fields=NCTId,BriefTitle,Condition&fmt=json&max_rnk={MaxSourceCount}";

    private sealed class PubMedSearchResponse
    {
        [JsonPropertyName("esearchresult")]
        public ESearchResult? SearchResult { get; set; }
    }

    private sealed class ESearchResult
    {
        [JsonPropertyName("idlist")]
        public List<string>? Ids { get; set; }
    }

    private sealed class ClinicalTrialsResponse
    {
        public StudyFieldsResponse? StudyFieldsResponse { get; set; }
    }

    private sealed class StudyFieldsResponse
    {
        public List<StudyFields>? StudyFields { get; set; }
    }

    private sealed class StudyFields
    {
        public List<string>? NctId { get; set; }
        public List<string>? BriefTitle { get; set; }
        public List<string>? Condition { get; set; }
    }
}
