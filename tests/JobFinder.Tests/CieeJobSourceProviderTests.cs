using System.Net;
using System.Text;
using JobFinder.Domain.Entities;
using JobFinder.Infrastructure.Sources;

namespace JobFinder.Tests;

public sealed class CieeJobSourceProviderTests
{
    [Fact]
    public async Task FetchMapsPublicCieeEditalToJobListingInput()
    {
        using var httpClient = new HttpClient(new CieeResponseHandler())
        {
            BaseAddress = new Uri("https://api-pp.ciee.org.br/")
        };
        var provider = new CieeJobSourceProvider(httpClient);

        var jobs = await provider.FetchAsync(CancellationToken.None);

        var job = Assert.Single(jobs);
        Assert.Equal("CIEE", job.Source);
        Assert.Equal("17480", job.ExternalId);
        Assert.Equal("Processo CIEE - Análise e Desenvolvimento de Sistemas", job.Title);
        Assert.Equal("Secretaria de Tecnologia", job.Company);
        Assert.Equal("São Paulo", job.Location);
        Assert.Equal(WorkMode.OnSite, job.WorkMode);
        Assert.Equal("R$937,59", job.Salary);
        Assert.Equal("Desenvolvimento", job.Category);
        Assert.Equal(
            "https://pp.ciee.org.br/vitrine/17480/detalhe",
            job.Url);
        Assert.Contains("dados", job.Description, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ProviderDescriptionDoesNotAdvertiseMockData()
    {
        var provider = new CieeJobSourceProvider(new HttpClient());
        Assert.Equal("CIEE", provider.Name);
        Assert.DoesNotContain("demonstr", provider.Description, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("example.com", provider.Description, StringComparison.OrdinalIgnoreCase);
    }

    private sealed class CieeResponseHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var json = request.RequestUri?.AbsolutePath.EndsWith("/api/editais/17480", StringComparison.Ordinal) == true
                ? """
                  {
                    "orgao": {"nome": "Secretaria de Tecnologia"},
                    "abrangencia": "MUNICIPAL",
                    "requisitos": {
                      "descricaoRequisito": "<p>Estágio em dados e sistemas.</p><p>Bolsa-auxílio: R$ 937,59 por mês.</p>"
                    },
                    "cronogramas": [
                      {"tipoFase": "INSCRICAO", "dataInicio": "2026-11-18T00:00:00"}
                    ],
                    "jornadas": [],
                    "editalCursos": [
                      {
                        "curso": {"descricao": "Análise e Desenvolvimento de Sistemas"}
                      }
                    ],
                    "vinculoLocaisCursos": [
                      {
                        "listLocais": [
                          {"cidade": "São Paulo", "uf": "SP"}
                        ]
                      }
                    ]
                  }
                  """
                : """
                  {
                    "content": [
                      {
                        "id": 17480,
                        "identificacao": "Edital 02/2026",
                        "ativo": true,
                        "status": "ABERTO",
                        "orgao": {"nome": "Secretaria de Tecnologia"}
                      }
                    ]
                  }
                  """;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            });
        }
    }
}
