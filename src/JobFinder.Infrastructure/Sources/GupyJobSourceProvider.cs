using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Infrastructure.Sources;

public sealed class GupyJobSourceProvider : IJobSourceProvider
{
    private const int LimitPerSearch = 30;
    private static readonly string[] SearchTerms =
    [
        "estagio", "estagiario", "qa", "qualidade de software",
        "desenvolvimento", "dados", "devops", "suporte tecnico", "seguranca da informacao"
    ];

    private readonly HttpClient _httpClient;

    public GupyJobSourceProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Gupy";
    public string Description => "Portal publico de vagas da Gupy, consultado pela API publica de busca.";
    public bool IsEnabled => true;

    public async Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken)
    {
        var jobsById = new Dictionary<string, JobListingInput>(StringComparer.Ordinal);

        foreach (var term in SearchTerms)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var relativeUrl = $"api/job-search/jobs?name={Uri.EscapeDataString(term)}&offset=0&limit={LimitPerSearch}&sortBy=publishedDate";
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
            response.EnsureSuccessStatusCode();
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var payload = await JsonSerializer.DeserializeAsync<GupySearchResponse>(stream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);

            foreach (var vacancy in payload?.Data ?? [])
            {
                if (vacancy.Id <= 0 || string.IsNullOrWhiteSpace(vacancy.JobUrl))
                    continue;

                var title = vacancy.Name?.Trim();
                var company = vacancy.CareerPageName?.Trim();
                if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(company))
                    continue;

                var externalId = vacancy.Id.ToString(CultureInfo.InvariantCulture);
                var location = BuildLocation(vacancy.City, vacancy.State);
                var description = StripHtml(vacancy.Description);
                if (vacancy.Disabilities)
                    description = $"{description}\nVaga com possibilidade de participação de pessoas com deficiência.";

                var job = new JobListingInput(
                    externalId,
                    title,
                    company,
                    location,
                    MapWorkMode(vacancy.WorkplaceType),
                    null,
                    Name,
                    NormalizeJobUrl(vacancy.JobUrl),
                    description,
                    InferCategory(title, description),
                    ParsePublishedAt(vacancy.PublishedDate));

                jobsById[externalId] = job;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150), cancellationToken);
        }

        return jobsById.Values.ToArray();
    }

    private static string BuildLocation(string? city, string? state)
    {
        var parts = new[] { city?.Trim(), state?.Trim() }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        return parts.Length == 0 ? "Brasil" : string.Join(", ", parts);
    }

    private static WorkMode MapWorkMode(string? workplaceType)
    {
        return workplaceType?.Trim().ToLowerInvariant() switch
        {
            "remote" or "remoto" or "home-office" or "home office" => WorkMode.Remote,
            "hybrid" or "hibrido" or "híbrido" => WorkMode.Hybrid,
            "on-site" or "onsite" or "presencial" => WorkMode.OnSite,
            _ => WorkMode.Unspecified
        };
    }

    private static string InferCategory(string title, string description)
    {
        var text = $"{title} {description}";
        if (Contains(text, "qa", "quality", "teste", "qualidade")) return "QA";
        if (Contains(text, "devops", "cloud", "ci/cd", "observabilidade")) return "DevOps";
        if (Contains(text, "seguranca", "cyber", "ciber", "security")) return "Segurança";
        if (Contains(text, "dados", "data", "sql", "business intelligence", "analytics")) return "Dados";
        if (Contains(text, "suporte", "help desk", "service desk")) return "Suporte";
        if (Contains(text, "desenvolvimento", "software", "programador", "developer", "programação")) return "Desenvolvimento";
        if (Contains(text, "engenharia")) return "Engenharia";
        return "Tecnologia da Informação";
    }

    private static bool Contains(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.OrdinalIgnoreCase));

    private static DateTimeOffset ParsePublishedAt(string? publishedDate)
    {
        return DateTimeOffset.TryParse(publishedDate, CultureInfo.InvariantCulture,
            DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var result)
            ? result.ToUniversalTime()
            : DateTimeOffset.UtcNow;
    }

    private static string NormalizeJobUrl(string jobUrl)
    {
        if (!Uri.TryCreate(jobUrl, UriKind.Absolute, out var uri) ||
            uri.Scheme is not ("http" or "https") ||
            !uri.Host.EndsWith(".gupy.io", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("A Gupy returned an invalid or unexpected job URL.");
        }
        return uri.ToString();
    }

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var decoded = WebUtility.HtmlDecode(html);
        var text = Regex.Replace(decoded, "<[^>]+>", " ", RegexOptions.Compiled);
        text = Regex.Replace(text, @"\s+", " ", RegexOptions.Compiled).Trim();
        return text.Length <= 10000 ? text : text[..10000];
    }

    private sealed class GupySearchResponse
    {
        [JsonPropertyName("data")]
        public GupyVacancy[]? Data { get; init; }
    }

    private sealed class GupyVacancy
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }
        [JsonPropertyName("name")]
        public string? Name { get; init; }
        [JsonPropertyName("description")]
        public string? Description { get; init; }
        [JsonPropertyName("careerPageName")]
        public string? CareerPageName { get; init; }
        [JsonPropertyName("publishedDate")]
        public string? PublishedDate { get; init; }
        [JsonPropertyName("workplaceType")]
        public string? WorkplaceType { get; init; }
        [JsonPropertyName("city")]
        public string? City { get; init; }
        [JsonPropertyName("state")]
        public string? State { get; init; }
        [JsonPropertyName("jobUrl")]
        public string? JobUrl { get; init; }
        [JsonPropertyName("disabilities")]
        public bool Disabilities { get; init; }
    }
}
