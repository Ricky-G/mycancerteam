using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Models;
using MyCancerTeam.Core.Research;

namespace MyCancerTeam.Infrastructure.Research;

public sealed class SearchApiResearchProvider : IResearchWebSearchProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly string _bingEndpoint;
    private readonly string _bingApiKey;
    private readonly string _serpApiEndpoint;
    private readonly string _serpApiKey;
    private readonly HttpClient _httpClient;

    private SearchApiResearchProvider(
        string bingEndpoint,
        string bingApiKey,
        string serpApiEndpoint,
        string serpApiKey,
        HttpClient httpClient)
    {
        _bingEndpoint = bingEndpoint;
        _bingApiKey = bingApiKey;
        _serpApiEndpoint = serpApiEndpoint;
        _serpApiKey = serpApiKey;
        _httpClient = httpClient;
    }

    public static SearchApiResearchProvider? TryCreate(AppConfiguration configuration, HttpClient httpClient)
    {
        if (string.IsNullOrWhiteSpace(configuration.BingWebSearchEndpoint) ||
            string.IsNullOrWhiteSpace(configuration.BingWebSearchKey) ||
            string.IsNullOrWhiteSpace(configuration.SerpApiEndpoint) ||
            string.IsNullOrWhiteSpace(configuration.SerpApiKey))
        {
            return null;
        }

        return new SearchApiResearchProvider(
            configuration.BingWebSearchEndpoint,
            configuration.BingWebSearchKey,
            configuration.SerpApiEndpoint,
            configuration.SerpApiKey,
            httpClient);
    }

    public async Task<ResearchEvidenceSnapshot> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        var warnings = new List<string>();
        var sources = new List<ResearchEvidenceSource>();

        sources.AddRange(await SearchBingAsync(query, warnings, cancellationToken));
        sources.AddRange(await SearchScholarAsync(query, warnings, cancellationToken));

        return new ResearchEvidenceSnapshot
        {
            Sources = sources
                .GroupBy(source => source.Citation.Url, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First())
                .ToArray(),
            Warnings = warnings.ToArray()
        };
    }

    private async Task<IReadOnlyList<ResearchEvidenceSource>> SearchBingAsync(
        string query,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_bingEndpoint}?q={Uri.EscapeDataString(query)}&count=3&responseFilter=Webpages");
        request.Headers.Add("Ocp-Apim-Subscription-Key", _bingApiKey);

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<BingSearchResponse>(JsonOptions, cancellationToken);
            return payload?.WebPages?.Value?
                .Where(item => !string.IsNullOrWhiteSpace(item.Url))
                .Select(item => new ResearchEvidenceSource
                {
                    Citation = new CitationMetadata
                    {
                        SourceName = "Bing Web Search",
                        Title = item.Name ?? item.Url!,
                        Url = item.Url!,
                        EvidenceLevel = "web search",
                        PublishedOn = ParseDate(item.DateLastCrawled)
                    },
                    Summary = item.Snippet ?? string.Empty
                })
                .ToArray() ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            warnings.Add($"Bing web search failed: {ex.Message}");
            return [];
        }
    }

    private async Task<IReadOnlyList<ResearchEvidenceSource>> SearchScholarAsync(
        string query,
        ICollection<string> warnings,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            $"{_serpApiEndpoint}?engine=google_scholar&q={Uri.EscapeDataString(query)}&api_key={Uri.EscapeDataString(_serpApiKey)}");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        try
        {
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var payload = await response.Content.ReadFromJsonAsync<SerpApiSearchResponse>(JsonOptions, cancellationToken);
            return payload?.OrganicResults?
                .Where(item => !string.IsNullOrWhiteSpace(item.Link))
                .Select(item => new ResearchEvidenceSource
                {
                    Citation = new CitationMetadata
                    {
                        SourceName = "SerpAPI Google Scholar",
                        Title = item.Title ?? item.Link!,
                        Url = item.Link!,
                        EvidenceLevel = "scholar search"
                    },
                    Summary = item.Snippet ?? item.PublicationInfo?.Summary ?? string.Empty
                })
                .ToArray() ?? [];
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException or TaskCanceledException)
        {
            warnings.Add($"SerpAPI scholar search failed: {ex.Message}");
            return [];
        }
    }

    private static DateOnly? ParseDate(string? value)
        => DateTimeOffset.TryParse(value, out var parsed) ? DateOnly.FromDateTime(parsed.UtcDateTime) : null;

    private sealed class BingSearchResponse
    {
        public BingWebPages? WebPages { get; set; }
    }

    private sealed class BingWebPages
    {
        public List<BingWebPage>? Value { get; set; }
    }

    private sealed class BingWebPage
    {
        public string? Name { get; set; }
        public string? Url { get; set; }
        public string? Snippet { get; set; }
        public string? DateLastCrawled { get; set; }
    }

    private sealed class SerpApiSearchResponse
    {
        [JsonPropertyName("organic_results")]
        public List<SerpApiResult>? OrganicResults { get; set; }
    }

    private sealed class SerpApiResult
    {
        public string? Title { get; set; }
        public string? Link { get; set; }
        public string? Snippet { get; set; }

        [JsonPropertyName("publication_info")]
        public PublicationInfo? PublicationInfo { get; set; }
    }

    private sealed class PublicationInfo
    {
        public string? Summary { get; set; }
    }
}
