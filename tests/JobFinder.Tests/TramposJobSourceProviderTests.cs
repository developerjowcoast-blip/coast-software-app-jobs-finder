using System.Net;
using System.Text;
using JobFinder.Domain.Entities;
using JobFinder.Infrastructure.Sources;

namespace JobFinder.Tests;

public sealed class TramposJobSourceProviderTests
{
    [Fact]
    public async Task FetchMapsPublicTramposDataWithoutMockUrl()
    {
        using var httpClient = new HttpClient(new TramposResponseHandler())
        {
            BaseAddress = new Uri("https://trampos.co/")
        };
        var provider = new TramposJobSourceProvider(httpClient);

        var jobs = await provider.FetchAsync(CancellationToken.None);

        var job = Assert.Single(jobs);
        Assert.Equal("Trampos", job.Source);
        Assert.Equal("775155", job.ExternalId);
        Assert.Equal("Analista de SEO Técnico", job.Title);
        Assert.Equal("Agência Lab8", job.Company);
        Assert.Equal("São Paulo, SP", job.Location);
        Assert.Equal(WorkMode.OnSite, job.WorkMode);
        Assert.Equal("R$ 4.000 a R$ 5.000", job.Salary);
        Assert.StartsWith("https://trampos.co/oportunidades/", job.Url);
        Assert.DoesNotContain("example.com", job.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("SEO", job.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProviderDescriptionDoesNotAdvertiseMockData()
    {
        var provider = new TramposJobSourceProvider(new HttpClient());
        Assert.Equal("Trampos", provider.Name);
        Assert.DoesNotContain("demonstr", provider.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example.com", provider.Description, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class TramposResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var json = path.EndsWith("/api/v2/opportunities/775155", StringComparison.Ordinal)
                ? """
                  {
                    "opportunity": {
                      "id": 775155,
                      "name": "Analista de SEO Técnico",
                      "category_name": "Marketing",
                      "state": "SP",
                      "city": "São Paulo",
                      "home_office": false,
                      "hybrid": false,
                      "published_at": "2026-11-20T13:35:06.000-03:00",
                      "salary": "R$ 4.000 a R$ 5.000",
                      "description": "SEO técnico para aumentar o tráfego e a relevância do site.",
                      "perks": "Bolsa de estudos",
                      "prerequisite": "Web Performance e VTEX",
                      "desirable": "Inglês fluente",
                      "other_info": "Vaga 100% presencial.",
                      "address": "Avenida Marquês de São Vicente, São Paulo",
                      "custom_company_name": null,
                      "url": "http://trampos.co/oportunidades/775155-analista-de-seo-tecnico",
                      "company": {"name": "Agência Lab8"}
                    }
                  }
                  """
                : """
                  [{
                    "opportunity": {
                      "id": 775155,
                      "name": "Analista de SEO Técnico",
                      "published_at": "2026-11-20 13:35:06 -0300",
                      "company_name": "Agência Lab8",
                      "permalink": "https://trampos.co/oportunidades/775155-analista-de-seo-tecnico"
                    }
                  }]
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
