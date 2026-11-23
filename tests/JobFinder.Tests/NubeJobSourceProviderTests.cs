using System.Net;
using System.Text;
using JobFinder.Domain.Entities;
using JobFinder.Infrastructure.Sources;

namespace JobFinder.Tests;

public sealed class NubeJobSourceProviderTests
{
    [Fact]
    public async Task FetchMapsPublicNubeDataWithoutMockUrl()
    {
        using var httpClient = new HttpClient(new NubeResponseHandler())
        {
            BaseAddress = new Uri("https://www.nube.com.br/")
        };
        var provider = new NubeJobSourceProvider(httpClient);

        var jobs = await provider.FetchAsync(CancellationToken.None);

        var job = Assert.Single(jobs);
        Assert.Equal("Nube", job.Source);
        Assert.Equal("390249", job.ExternalId);
        Assert.Equal("Análise de Dados", job.Title);
        Assert.Equal("São Paulo, SP", job.Location);
        Assert.Equal(WorkMode.OnSite, job.WorkMode);
        Assert.Equal("R$ 1.621,00", job.Salary);
        Assert.StartsWith("https://www.nube.com.br/detalhes-vaga/", job.Url);
        Assert.DoesNotContain("example.com", job.Url, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("dados", job.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProviderDescriptionDoesNotAdvertiseMockData()
    {
        var provider = new NubeJobSourceProvider(new HttpClient());
        Assert.Equal("Nube", provider.Name);
        Assert.DoesNotContain("demonstr", provider.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example.com", provider.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DuplicateUrlAndTitleCompanyAreRemoved()
    {
        var inputs = new[]
        {
            new JobFinder.Application.Contracts.JobListingInput(
                "1", "Análise de Dados", "Empresa A", "São Paulo", WorkMode.OnSite, null,
                "Nube", "https://www.nube.com.br/detalhes-vaga/1", "Dados", "Dados",
                DateTimeOffset.UtcNow),
            new JobFinder.Application.Contracts.JobListingInput(
                "2", "Análise de Dados", "Empresa A", "São Paulo", WorkMode.OnSite, null,
                "Nube", "https://www.nube.com.br/detalhes-vaga/1", "Dados duplicados", "Dados",
                DateTimeOffset.UtcNow),
            new JobFinder.Application.Contracts.JobListingInput(
                "3", "Análise de Dados", "Empresa A", "São Paulo", WorkMode.Remote, null,
                "Nube", "https://www.nube.com.br/detalhes-vaga/3", "Dados duplicados", "Dados",
                DateTimeOffset.UtcNow)
        };

        static string Normalize(string value) =>
            value.Trim().ToLowerInvariant();
        var deduplicated = inputs
            .DistinctBy(input => input.Url.TrimEnd('/').ToLowerInvariant())
            .DistinctBy(input => $"{Normalize(input.Title)}|{Normalize(input.Company)}")
            .ToArray();

        Assert.Single(deduplicated);
    }

    private sealed class NubeResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = request.RequestUri?.AbsolutePath.Contains("buscar-detalhes-vaga") == true
                ? """
                  {
                    "status": 200,
                    "param": {
                      "share": {
                        "sumario": "Atualizar e organizar bases de dados e planilhas."
                      },
                      "_jobposting_sch": "<p>Atividades com dados e sistemas.</p>",
                      "texto_curso_destaque": "Análise de Dados"
                    }
                  }
                  """
                : """
                  {
                    "status": 200,
                    "param": {
                      "dict_por_id_vaga": {
                        "390249": {
                          "view": {
                            "id_vaga": 390249,
                            "titulo": "Análise de Dados - 390249",
                            "tipo_vaga": "Vaga de Aprendiz",
                            "url": "/detalhes-vaga/390249/vaga-de-aprendiz-em-analise-de-dados-sao-paulo-sp",
                            "cidade": "São Paulo",
                            "uf": "SP",
                            "local": "São Paulo | SP",
                            "bolsa_valor": "R$ 1.621,00",
                            "id_modalidade_atuacao": 1,
                            "inclusao": 1,
                            "beneficios": [
                              {"mostrar": true, "titulo": "Vale-refeição"}
                            ]
                          },
                          "vaga": {
                            "regex_filtro": "|Analise de Dados|Aprendiz|Sao Paulo|SP"
                          }
                        }
                      }
                    }
                  }
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
