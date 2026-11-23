using System.Net;
using System.Text;
using JobFinder.Domain.Entities;
using JobFinder.Infrastructure.Sources;

namespace JobFinder.Tests;

public sealed class GupyJobSourceProviderTests
{
    [Fact]
    public async Task FetchMapsRealGupyJsonToJobListingInput()
    {
        using var httpClient = new HttpClient(new GupyResponseHandler())
        {
            BaseAddress = new Uri("https://portal.gupy.io/")
        };
        var provider = new GupyJobSourceProvider(httpClient);

        var jobs = await provider.FetchAsync(CancellationToken.None);

        var job = Assert.Single(jobs, item => item.ExternalId == "12345");
        Assert.Equal("Gupy", job.Source);
        Assert.Equal("Estágio em QA", job.Title);
        Assert.Equal("Empresa Tecnologia", job.Company);
        Assert.Equal("São Paulo", job.Location);
        Assert.Equal(WorkMode.Hybrid, job.WorkMode);
        Assert.Equal("QA", job.Category);
        Assert.Contains("Quality Assurance", job.Description);
        Assert.StartsWith("https://empresa.gupy.io/job/", job.Url);
    }

    [Fact]
    public void ProviderUsesNoExampleDotComUrls()
    {
        var provider = new GupyJobSourceProvider(new HttpClient());
        Assert.Equal("Gupy", provider.Name);
        Assert.DoesNotContain("example.com", provider.Description, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class GupyResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            const string json = """
            {
              "data": [{
                "id": 12345,
                "name": "Estágio em QA",
                "description": "<p>Estágio para aprender <strong>Quality Assurance</strong> e testes.</p>",
                "careerPageName": "Empresa Tecnologia",
                "publishedDate": "2026-11-19T12:00:00Z",
                "workplaceType": "hybrid",
                "city": "São Paulo",
                "state": "São Paulo",
                "jobUrl": "https://empresa.gupy.io/job/abc123",
                "disabilities": true
              }],
              "pagination": { "total": 1, "limit": 30, "offset": 0 }
            }
            """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
