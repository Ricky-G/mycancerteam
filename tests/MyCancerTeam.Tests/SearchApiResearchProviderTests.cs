using System.Net;
using System.Text;
using MyCancerTeam.Core.Configuration;
using MyCancerTeam.Infrastructure.Research;

namespace MyCancerTeam.Tests;

public sealed class SearchApiResearchProviderTests
{
    [Fact]
    public async Task SearchAsync_ShouldReturnBingAndScholarResults()
    {
        var config = new AppConfiguration
        {
            BingWebSearchEndpoint = "https://api.bing.microsoft.com/v7.0/search",
            BingWebSearchKey = "bing-key",
            SerpApiEndpoint = "https://serpapi.com/search.json",
            SerpApiKey = "serp-key"
        };

        var httpClient = new HttpClient(new SearchApiHandler());
        var provider = SearchApiResearchProvider.TryCreate(config, httpClient);

        Assert.NotNull(provider);

        var snapshot = await provider!.SearchAsync("metastatic melanoma");

        Assert.Equal(2, snapshot.Sources.Count);
        Assert.Contains(snapshot.Sources, source => source.Citation.SourceName == "Bing Web Search");
        Assert.Contains(snapshot.Sources, source => source.Citation.SourceName == "SerpAPI Google Scholar");
        Assert.Empty(snapshot.Warnings);
    }

    private sealed class SearchApiHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var uri = request.RequestUri?.ToString() ?? string.Empty;

            if (uri.Contains("api.bing.microsoft.com", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateResponse("""
                {
                  "webPages": {
                    "value": [
                      {
                        "name": "Bing search result",
                        "url": "https://example.com/bing",
                        "snippet": "Bing result snippet.",
                        "dateLastCrawled": "2026-06-01T00:00:00Z"
                      }
                    ]
                  }
                }
                """));
            }

            if (uri.Contains("serpapi.com", StringComparison.OrdinalIgnoreCase))
            {
                return Task.FromResult(CreateResponse("""
                {
                  "organic_results": [
                    {
                      "title": "Scholar search result",
                      "link": "https://example.com/scholar",
                      "snippet": "Scholar result snippet.",
                      "publication_info": {
                        "summary": "2026" 
                      }
                    }
                  ]
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
