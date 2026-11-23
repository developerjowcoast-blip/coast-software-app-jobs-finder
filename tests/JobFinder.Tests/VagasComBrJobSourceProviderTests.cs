using System.Net;
using System.Text;
using JobFinder.Domain.Entities;
using JobFinder.Infrastructure.Sources;

namespace JobFinder.Tests;

public sealed class VagasComBrJobSourceProviderTests
{
    [Fact]
    public async Task FetchMapsPublicVagasComBrHtmlToJobListingInput()
    {
        using var httpClient = new HttpClient(new VagasComBrResponseHandler())
        {
            BaseAddress = new Uri("https://www.vagas.com.br/")
        };
        var provider = new VagasComBrJobSourceProvider(httpClient);

        var jobs = await provider.FetchAsync(CancellationToken.None);

        var job = Assert.Single(jobs, item => item.ExternalId == "2783038");
        Assert.Equal("Vagas.com.br", job.Source);
        Assert.Equal("Programa de Estágio B3 - 2026", job.Title);
        Assert.Equal("B3", job.Company);
        Assert.Equal("São Paulo", job.Location);
        Assert.Equal(WorkMode.OnSite, job.WorkMode);
        Assert.Equal("R$ 2.300", job.Salary);
        Assert.Equal("Dados", job.Category);
        Assert.StartsWith("https://www.vagas.com.br/vagas/v2783038/", job.Url);
        Assert.DoesNotContain("example.com", job.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Tecnologia da Informação", job.Description);
    }

    [Fact]
    public void ProviderDescriptionDoesNotAdvertiseMockData()
    {
        var provider = new VagasComBrJobSourceProvider(new HttpClient());
        Assert.Equal("Vagas.com.br", provider.Name);
        Assert.DoesNotContain("demonstr", provider.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example.com", provider.Description, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class VagasComBrResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var path = request.RequestUri?.AbsolutePath ?? string.Empty;
            var html = path.StartsWith("/vagas/v2783038/", StringComparison.OrdinalIgnoreCase)
                ? """
                  <!doctype html>
                  <html>
                    <head>
                      <meta property="og:title" content="Programa de Estágio B3 - 2026 - B3 | Vagas.com">
                    </head>
                    <body>
                      <h1 class="job-shortdescription__title">Programa de Estágio B3 - 2026</h1>
                      <h2 class="job-shortdescription__company">B3</h2>
                      <li>
                        <span class="info-localizacao">São Paulo</span>
                      </li>
                      <li>
                        <span>Faixa salarial</span>
                        <span><b>R$ 2.300</b></span>
                      </li>
                      <div data-testid="JobDescription">
                        <p>Programa de estágio em Tecnologia da Informação, desenvolvimento de software e dados.</p>
                      </div>
                      Publicada em 01/01/2026
                    </body>
                  </html>
                  """
                : """
                  <div class="job-card">
                    <h2><a href="/vagas/v2783038/programa-de-estagio-b3-2026" title="Programa de Estágio B3 - 2026">Programa de Estágio B3 - 2026</a></h2>
                    <span>B3</span>
                  </div>
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html, Encoding.UTF8, "text/html")
            });
        }
    }
}
