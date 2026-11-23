using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Infrastructure.Sources;

public sealed class TramposJobSourceProvider : IJobSourceProvider
{
    private const int MaximumPages = 5;
    private readonly HttpClient _httpClient;

    public TramposJobSourceProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Trampos";
    public string Description => "API publica do Trampos usada para coletar oportunidades reais.";
    public bool IsEnabled => true;

    public async Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken)
    {
        var jobsByUrl = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);
        var jobsByTitleCompany = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);

        for (var page = 1; page <= MaximumPages; page++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var listResponse = await _httpClient.GetAsync(
                $"api/oportunidades?page={page}", cancellationToken);
            listResponse.EnsureSuccessStatusCode();
            await using var listStream = await listResponse.Content.ReadAsStreamAsync(cancellationToken);
            var listings = await JsonSerializer.DeserializeAsync<List<TramposListing>>(
                listStream,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                cancellationToken);
            if (listings is null || listings.Count == 0)
                break;

            foreach (var listing in listings)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var summary = listing.Opportunity;
                if (summary is null || summary.Id <= 0)
                    continue;

                TramposOpportunity? detail = null;
                try
                {
                    using var detailResponse = await _httpClient.GetAsync(
                        $"api/v2/opportunities/{summary.Id}", cancellationToken);
                    if (detailResponse.IsSuccessStatusCode)
                    {
                        await using var detailStream = await detailResponse.Content.ReadAsStreamAsync(cancellationToken);
                        var detailPayload = await JsonSerializer.DeserializeAsync<TramposDetail>(
                            detailStream,
                            new JsonSerializerOptions { PropertyNameCaseInsensitive = true },
                            cancellationToken);
                        detail = detailPayload?.Opportunity;
                    }
                }
                catch (HttpRequestException)
                {
                }

                var opportunity = detail ?? summary;
                var title = NormalizeTitle(opportunity.Name);
                var company = NormalizeCompany(opportunity.CustomCompanyName, opportunity.Company?.Name, summary.CompanyName);
                var location = BuildLocation(opportunity.City, opportunity.State, opportunity.Address);
                var description = BuildDescription(opportunity);
                var text = $"{title} {opportunity.CategoryName} {description}";

                if (!IsRelevantTechnologyRole(text))
                    continue;

                var jobUrl = NormalizeJobUrl(opportunity.Url, summary.Permalink, opportunity.Id);
                var job = new JobListingInput(
                    opportunity.Id.ToString(CultureInfo.InvariantCulture),
                    title,
                    company,
                    location,
                    MapWorkMode(opportunity),
                    NormalizeSalary(opportunity.Salary),
                    Name,
                    jobUrl,
                    description,
                    InferCategory(text, description),
                    ParsePublishedAt(opportunity.PublishedAt, summary.PublishedAt));

                var urlKey = NormalizeUrl(job.Url);
                var titleCompanyKey = BuildTitleCompanyKey(job.Title, job.Company);
                if (!jobsByUrl.ContainsKey(urlKey) && !jobsByTitleCompany.ContainsKey(titleCompanyKey))
                {
                    jobsByUrl[urlKey] = job;
                    jobsByTitleCompany[titleCompanyKey] = job;
                }

                await Task.Delay(TimeSpan.FromMilliseconds(40), cancellationToken);
            }
        }

        return jobsByUrl.Values.ToArray();
    }

    private static string NormalizeTitle(string? title)
    {
        var normalized = Regex.Replace(title?.Trim() ?? "Oportunidade Trampos", @"\s+", " ");
        return normalized.Length == 0 ? "Oportunidade Trampos" : normalized;
    }

    private static string NormalizeCompany(string? customCompany, string? detailCompany, string? listCompany)
    {
        foreach (var company in new[] { customCompany, detailCompany, listCompany })
        {
            if (!string.IsNullOrWhiteSpace(company))
                return Regex.Replace(company.Trim(), @"\s+", " ");
        }
        return "Empresa não informada";
    }

    private static string BuildLocation(string? city, string? state, string? address)
    {
        var cityState = new[] { city?.Trim(), state?.Trim() }
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (cityState.Length > 0)
            return string.Join(", ", cityState);
        if (!string.IsNullOrWhiteSpace(address))
            return address.Trim();
        return "Brasil";
    }

    private static WorkMode MapWorkMode(TramposOpportunity opportunity)
    {
        var text = $"{opportunity.Description} {opportunity.OtherInfo} {opportunity.Address}".ToLowerInvariant();
        if (text.Contains("100% remoto") || text.Contains("home office") ||
            text.Contains("remoto") || opportunity.HomeOffice)
            return WorkMode.Remote;
        if (text.Contains("híbrido") || text.Contains("hibrido") || opportunity.Hybrid)
            return WorkMode.Hybrid;
        return WorkMode.OnSite;
    }

    private static string? NormalizeSalary(string? salary)
    {
        return string.IsNullOrWhiteSpace(salary) ? null : Regex.Replace(salary.Trim(), @"\s+", " ");
    }

    private static string BuildDescription(TramposOpportunity opportunity)
    {
        var pieces = new[]
        {
            opportunity.Description,
            opportunity.Prerequisite,
            opportunity.Desirable,
            opportunity.OtherInfo,
            opportunity.Perks
        }.Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(StripText)
            .Where(value => !string.IsNullOrWhiteSpace(value));
        var description = string.Join(" ", pieces);
        return string.IsNullOrWhiteSpace(description)
            ? $"Oportunidade de {opportunity.CategoryName ?? "tecnologia"} na Trampos."
            : description.Length <= 10000 ? description : description[..10000];
    }

    private static string StripText(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var text = Regex.Replace(WebUtility.HtmlDecode(value), @"\r?\n", " ");
        text = Regex.Replace(text, @"<[^>]+>", " ");
        return Regex.Replace(text, @"\s+", " ").Trim();
    }

    private static DateTimeOffset ParsePublishedAt(string? detailDate, string? listDate)
    {
        foreach (var value in new[] { detailDate, listDate })
        {
            if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var result))
                return result.ToUniversalTime();
        }
        return DateTimeOffset.UtcNow;
    }

    private static bool IsRelevantTechnologyRole(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        string[] terms =
        [
            "tecnologia", "desenvolvimento", "desenvolvedor", "developer",
            "software", "programacao", "qa", "qualidade", "teste",
            "dados", "data", "business intelligence", "bi",
            "devops", "cloud", "seguranca", "cyber", "suporte",
            "informatica", "sistemas", "engenharia de software",
            "seo tecnico", "sql", "machine learning"
        ];
        return terms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }

    private static string InferCategory(string text, string description)
    {
        var combined = RemoveDiacritics($"{text} {description}".ToLowerInvariant());
        if (Contains(combined, "qa", "qualidade", "teste")) return "QA";
        if (Contains(combined, "devops", "cloud", "observabilidade")) return "DevOps";
        if (Contains(combined, "seguranca", "cyber", "security")) return "Segurança";
        if (Contains(combined, "dados", "data", "business intelligence", "sql", "analytics")) return "Dados";
        if (Contains(combined, "suporte", "help desk")) return "Suporte";
        if (Contains(combined, "desenvolvimento", "software", "programacao", "sistemas")) return "Desenvolvimento";
        if (Contains(combined, "seo")) return "Marketing";
        return "Tecnologia da Informação";
    }

    private static bool Contains(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));

    private static string NormalizeJobUrl(string? detailUrl, string? summaryUrl, long id)
    {
        var candidate = detailUrl ?? summaryUrl;
        if (!string.IsNullOrWhiteSpace(candidate) &&
            Uri.TryCreate(candidate, UriKind.Absolute, out var uri) &&
            uri.Scheme is "http" or "https")
        {
            return new UriBuilder(uri) { Scheme = Uri.UriSchemeHttps, Port = -1 }.Uri.ToString();
        }
        return $"https://trampos.co/oportunidades/{id}";
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.TrimEnd('/').ToLowerInvariant();
        var builder = new UriBuilder(uri) { Fragment = string.Empty, Query = string.Empty };
        return builder.Uri.ToString().TrimEnd('/').ToLowerInvariant();
    }

    private static string BuildTitleCompanyKey(string title, string company)
    {
        return $"{RemoveDiacritics(title).Trim().ToLowerInvariant()}|{RemoveDiacritics(company).Trim().ToLowerInvariant()}";
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }

    private sealed class TramposListing
    {
        [JsonPropertyName("opportunity")]
        public TramposOpportunity? Opportunity { get; init; }
    }

    private sealed class TramposDetail
    {
        [JsonPropertyName("opportunity")]
        public TramposOpportunity? Opportunity { get; init; }
    }

    private sealed class TramposOpportunity
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }
        [JsonPropertyName("name")]
        public string? Name { get; init; }
        [JsonPropertyName("category_name")]
        public string? CategoryName { get; init; }
        [JsonPropertyName("state")]
        public string? State { get; init; }
        [JsonPropertyName("city")]
        public string? City { get; init; }
        [JsonPropertyName("home_office")]
        public bool HomeOffice { get; init; }
        [JsonPropertyName("hybrid")]
        public bool Hybrid { get; init; }
        [JsonPropertyName("published_at")]
        public string? PublishedAt { get; init; }
        [JsonPropertyName("salary")]
        public string? Salary { get; init; }
        [JsonPropertyName("description")]
        public string? Description { get; init; }
        [JsonPropertyName("perks")]
        public string? Perks { get; init; }
        [JsonPropertyName("prerequisite")]
        public string? Prerequisite { get; init; }
        [JsonPropertyName("desirable")]
        public string? Desirable { get; init; }
        [JsonPropertyName("other_info")]
        public string? OtherInfo { get; init; }
        [JsonPropertyName("address")]
        public string? Address { get; init; }
        [JsonPropertyName("custom_company_name")]
        public string? CustomCompanyName { get; init; }
        [JsonPropertyName("url")]
        public string? Url { get; init; }
        [JsonPropertyName("permalink")]
        public string? Permalink { get; init; }
        [JsonPropertyName("company_name")]
        public string? CompanyName { get; init; }
        [JsonPropertyName("company")]
        public TramposCompany? Company { get; init; }
    }

    private sealed class TramposCompany
    {
        [JsonPropertyName("name")]
        public string? Name { get; init; }
    }
}
