using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Infrastructure.Sources;

public sealed class VagasComBrJobSourceProvider : IJobSourceProvider
{
    private const int MaximumPages = 3;
    private static readonly string[] SearchTerms =
    [
        "estagio", "tecnologia-da-informacao", "dados", "desenvolvimento",
        "qa", "devops", "suporte-tecnico", "seguranca-da-informacao"
    ];

    private readonly HttpClient _httpClient;

    public VagasComBrJobSourceProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Vagas.com.br";
    public string Description => "Pesquisa publica do Vagas.com.br com leitura dos anuncios publicos.";
    public bool IsEnabled => true;

    public async Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken)
    {
        var jobsByUrl = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);
        var jobsByTitleCompany = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);

        foreach (var term in SearchTerms)
        {
            for (var page = 1; page <= MaximumPages; page++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var response = await _httpClient.GetAsync(
                    $"vagas-de-{term}?ordenar_por=mais_relevantes&pagina={page}",
                    cancellationToken);
                if (!response.IsSuccessStatusCode)
                    break;

                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                var links = ExtractSearchLinks(html);
                if (links.Count == 0)
                    break;

                foreach (var link in links)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var detail = await FetchDetailAsync(link.Url, link.ExternalId, cancellationToken);
                    if (detail is null)
                        continue;

                    var text = $"{detail.Title} {detail.Description} {detail.Category}";
                    if (!IsRelevantTechnologyRole(text))
                        continue;

                    var urlKey = NormalizeUrl(detail.Url);
                    var titleCompanyKey = BuildTitleCompanyKey(detail.Title, detail.Company);
                    if (!jobsByUrl.ContainsKey(urlKey) && !jobsByTitleCompany.ContainsKey(titleCompanyKey))
                    {
                        jobsByUrl[urlKey] = detail;
                        jobsByTitleCompany[titleCompanyKey] = detail;
                    }

                    await Task.Delay(TimeSpan.FromMilliseconds(30), cancellationToken);
                }

                await Task.Delay(TimeSpan.FromMilliseconds(80), cancellationToken);
            }
        }

        return jobsByUrl.Values.ToArray();
    }

    private async Task<JobListingInput?> FetchDetailAsync(
        string relativeUrl,
        string externalId,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _httpClient.GetAsync(relativeUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return null;
            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            var title = ExtractTagText(html, "job-shortdescription__title")
                ?? ExtractMeta(html, "og:title");
            title = CleanTitle(title);
            if (string.IsNullOrWhiteSpace(title))
                return null;

            var company = ExtractTagText(html, "job-shortdescription__company")
                ?? ExtractTagText(html, "job-card__company");
            company = string.IsNullOrWhiteSpace(company) ? "Empresa não informada" : company.Trim();

            var location = ExtractClassValue(html, "info-localizacao");
            if (string.IsNullOrWhiteSpace(location))
                location = ExtractMeta(html, "og:region") ?? "Brasil";

            var salary = ExtractClassValue(html, "icone-salario", "b");
            if (string.IsNullOrWhiteSpace(salary))
                salary = ExtractSalary(html);

            var description = ExtractDescription(html);
            if (string.IsNullOrWhiteSpace(description))
                description = ExtractMeta(html, "description") ?? $"Vaga {title} publicada no Vagas.com.br.";

            var absoluteUrl = new Uri(_httpClient.BaseAddress!, relativeUrl.TrimStart('/')).ToString();
            var category = InferCategory(title, description);
            var workMode = InferWorkMode($"{location} {description}");
            var publishedAt = ExtractPublishedAt(html);

            return new JobListingInput(
                externalId,
                title,
                company,
                location,
                workMode,
                salary,
                Name,
                absoluteUrl,
                StripText(description),
                category,
                publishedAt);
        }
        catch (HttpRequestException)
        {
            return null;
        }
    }

    private static IReadOnlyList<(string ExternalId, string Url)> ExtractSearchLinks(string html)
    {
        var matches = Regex.Matches(
            html,
            @"<a[^>]+href=""(?<url>/vagas/v(?<id>\d+)/[^""]+)""[^>]*>",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<(string ExternalId, string Url)>();
        foreach (Match match in matches)
        {
            var id = match.Groups["id"].Value;
            var url = match.Groups["url"].Value;
            if (seen.Add($"{id}|{url}"))
                result.Add((id, url));
        }
        return result;
    }

    private static string? ExtractMeta(string html, string property)
    {
        var patterns = new[]
        {
            $@"<meta[^>]+(?:property|name)=""{Regex.Escape(property)}""[^>]+content=""([^""]+)""",
            $@"<meta[^>]+content=""([^""]+)""[^>]+(?:property|name)=""{Regex.Escape(property)}"""
        };
        foreach (var pattern in patterns)
        {
            var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
            if (match.Success)
                return WebUtility.HtmlDecode(match.Groups[1].Value).Trim();
        }
        return null;
    }

    private static string? ExtractTagText(string html, string cssClass)
    {
        var match = Regex.Match(
            html,
            "<[^>]+class=\"[^\"]*" + Regex.Escape(cssClass) + "[^\"]*\"[^>]*>(?<content>.*?)</[^>]+>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        return match.Success ? StripText(match.Groups["content"].Value) : null;
    }

    private static string? ExtractClassValue(string html, string cssClass, string? preferredTag = null)
    {
        var tagPattern = preferredTag is null
            ? @"[^>]*"
            : preferredTag;
        var match = Regex.Match(
            html,
            "<[^>]+class=\"[^\"]*" + Regex.Escape(cssClass) + "[^\"]*\"[^>]*>(?<content>.*?)(?:</" + tagPattern + @">|</[^>]+>)",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            match = Regex.Match(
                html,
                "class=\"[^\"]*" + Regex.Escape(cssClass) + "[^\"]*\"[^>]*>(?<content>.*?)</",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
        return match.Success ? StripText(match.Groups["content"].Value) : null;
    }

    private static string? ExtractDescription(string html)
    {
        var match = Regex.Match(
            html,
            @"<div[^>]+data-testid=""JobDescription""[^>]*>(?<content>.*?)</div>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (!match.Success)
        {
            match = Regex.Match(
                html,
                @"<div[^>]+class=""[^""]*job-description__text[^""]*""[^>]*>(?<content>.*?)</div>",
                RegexOptions.IgnoreCase | RegexOptions.Singleline);
        }
        return match.Success ? StripText(match.Groups["content"].Value) : null;
    }

    private static string? ExtractSalary(string html)
    {
        var match = Regex.Match(
            html,
            @"Faixa salarial\s*</span>\s*<span>\s*<b>(?<salary>.*?)</b>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);
        if (match.Success)
            return StripText(match.Groups["salary"].Value);

        match = Regex.Match(
            html,
            @">(R\$\s*[0-9][0-9.\s]*(?:,[0-9]+)?)<",
            RegexOptions.IgnoreCase);
        return match.Success ? Regex.Replace(match.Groups[1].Value, @"\s+", " ").Trim() : null;
    }

    private static DateTimeOffset ExtractPublishedAt(string html)
    {
        var match = Regex.Match(
            html,
            @"Publicada em\s+(\d{2}/\d{2}/\d{4})",
            RegexOptions.IgnoreCase);
        if (match.Success &&
            DateTimeOffset.TryParseExact(match.Groups[1].Value, "dd/MM/yyyy",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var date))
            return date;
        return DateTimeOffset.UtcNow;
    }

    private static string CleanTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return string.Empty;
        title = WebUtility.HtmlDecode(title);
        if (title.EndsWith(" | Vagas.com", StringComparison.OrdinalIgnoreCase))
            title = title[..^" | Vagas.com".Length];
        return Regex.Replace(title, @"\s+", " ").Trim();
    }

    private static WorkMode InferWorkMode(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        if (normalized.Contains("100% remoto") || normalized.Contains("home office") ||
            normalized.Contains("trabalho remoto"))
            return WorkMode.Remote;
        if (normalized.Contains("hibrido") || normalized.Contains("modelo hibrido"))
            return WorkMode.Hybrid;
        return WorkMode.OnSite;
    }

    private static bool IsRelevantTechnologyRole(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        string[] terms =
        [
            "tecnologia", "ti", "software", "desenvolvimento", "programacao",
            "programador", "dados", "data", "business intelligence",
            "qa", "qualidade", "teste", "devops", "cloud", "seguranca",
            "cyber", "suporte", "sistemas", "engenharia de software",
            "informatica", "computacao", "sql", "machine learning"
        ];
        return terms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }

    private static string InferCategory(string title, string description)
    {
        var combined = RemoveDiacritics($"{title} {description}".ToLowerInvariant());
        if (Contains(combined, "qa", "qualidade", "teste")) return "QA";
        if (Contains(combined, "devops", "cloud", "observabilidade")) return "DevOps";
        if (Contains(combined, "seguranca", "cyber", "security")) return "Segurança";
        if (Contains(combined, "dados", "data", "business intelligence", "sql", "analytics")) return "Dados";
        if (Contains(combined, "suporte", "help desk", "service desk")) return "Suporte";
        if (Contains(combined, "desenvolvimento", "software", "programacao", "sistemas")) return "Desenvolvimento";
        return "Tecnologia da Informação";
    }

    private static bool Contains(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));

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

    private static string StripText(string value)
    {
        var text = Regex.Replace(WebUtility.HtmlDecode(value), "<[^>]+>", " ");
        text = Regex.Replace(text, @"&nbsp;", " ", RegexOptions.IgnoreCase);
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= 10000 ? text : text[..10000];
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }
}
