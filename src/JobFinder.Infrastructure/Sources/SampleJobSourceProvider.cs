using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Infrastructure.Sources;

public sealed class SampleJobSourceProvider(string name) : IJobSourceProvider
{
    public string Name { get; } = name;
    public string Description => "Feed de demonstração com vagas públicas fictícias para validar o fluxo local.";
    public bool IsEnabled => true;

    public Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var now = DateTimeOffset.UtcNow;
        var index = Name.Sum(character => char.ToUpperInvariant(character)) % SampleJobs.Length;
        var definition = SampleJobs[index];
        IReadOnlyCollection<JobListingInput> jobs =
        [
            new JobListingInput(
                $"sample-{definition.Slug}", definition.Title, definition.Company, definition.Location,
                definition.Mode, definition.Salary, Name, $"https://example.com/jobs/{definition.Slug}",
                definition.Description, definition.Category, now.AddDays(-index))
        ];
        return Task.FromResult(jobs);
    }

    private static readonly (string Slug, string Title, string Company, string Location, WorkMode Mode, string Salary, string Description, string Category)[] SampleJobs =
    [
        ("qa-intern-01", "QA Intern - Spanish/LatAm", "Nexa Digital", "São Paulo, SP", WorkMode.Hybrid, "R$ 1.800–2.200",
            "Estágio em Quality Assurance para apoiar testes manuais e automação. Espanhol para contato com o time LATAM. Programa inclusivo; não exige experiência formal.", "QA"),
        ("dev-intern-02", "Estágio em Desenvolvimento de Software", "Aurora Tech", "Brasil", WorkMode.Remote, "R$ 2.000",
            "Oportunidade para estudante de ADS colaborar com um time global de engenharia e aprender desenvolvimento web.", "Desenvolvimento"),
        ("support-intern-03", "Estágio de Suporte Técnico Bilíngue", "Atlas Cloud", "São Paulo, SP", WorkMode.OnSite, "R$ 1.600",
            "Suporte técnico para clientes da América Latina. Espanhol profissional e interesse por cloud são valorizados.", "Suporte"),
        ("data-intern-04", "Estágio em Dados e Business Intelligence", "Ponte Analytics", "São Paulo, SP", WorkMode.Hybrid, "A combinar",
            "Vaga de entrada para estudantes de tecnologia com interesse em SQL, relatórios e análise de dados.", "Dados"),
        ("devops-intern-05", "DevOps Intern", "Nuvem Aberta", "Brasil", WorkMode.Remote, "R$ 2.100",
            "Programa de estágio inclusivo para aprender CI/CD, cloud e observabilidade com uma equipe diversa.", "DevOps"),
        ("security-intern-06", "Estágio em Segurança da Informação", "Cobalto Sistemas", "São Paulo, SP", WorkMode.Hybrid, "R$ 1.900",
            "Apoio a atividades de segurança, revisão de controles e conscientização. Estudantes de ADS são bem-vindos.", "Segurança")
    ];
}
