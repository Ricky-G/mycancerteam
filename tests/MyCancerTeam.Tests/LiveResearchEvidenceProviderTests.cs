using System.Net;
using System.Text;
using MyCancerTeam.Core.Agents;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Core.Models;
using MyCancerTeam.Core.Research;
using MyCancerTeam.Infrastructure.Research;

namespace MyCancerTeam.Tests;

public sealed class LiveResearchEvidenceProviderTests
{
    [Fact]
    public async Task GetEvidenceAsync_ShouldMergeSearchApiResultsIntoSnapshot()
    {
        var webSource = new ResearchEvidenceSource
        {
            Citation = new CitationMetadata
            {
                SourceName = "Bing Web Search",
                Title = "Live search source",
                Url = "https://example.com/live-search",
                EvidenceLevel = "web search"
            },
            Summary = "Live search snippet."
        };

        var provider = new LiveResearchEvidenceProvider(new StubWebSearchProvider(webSource), new HttpClient(new EmptyResearchHandler()));

        var snapshot = await provider.GetEvidenceAsync("metastatic melanoma");

        Assert.Contains(snapshot.Sources, source => source.Citation.Url == "https://example.com/live-search");
        Assert.Contains(snapshot.Warnings, warning => warning.Contains("PubMed", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(snapshot.Warnings, warning => warning.Contains("ClinicalTrials", StringComparison.OrdinalIgnoreCase));
    }

    private sealed class StubWebSearchProvider : IResearchWebSearchProvider
    {
        private readonly ResearchEvidenceSource _source;

        public StubWebSearchProvider(ResearchEvidenceSource source)
        {
            _source = source;
        }

        public Task<ResearchEvidenceSnapshot> SearchAsync(string query, CancellationToken cancellationToken = default)
            => Task.FromResult(new ResearchEvidenceSnapshot { Sources = [_source] });
    }

    private sealed class EmptyResearchHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;
            if (uri.Contains("esearch.fcgi", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateResponse("""
                {
                  "esearchresult": {
                    "idlist": []
                  }
                }
                """));
            }

            if (uri.Contains("study_fields", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateResponse("""
                {
                  "StudyFieldsResponse": {
                    "StudyFields": []
                  }
                }
                """));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }

        private static HttpResponseMessage CreateResponse(string json)
            => new(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };
    }
}
