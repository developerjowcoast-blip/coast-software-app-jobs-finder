using System.Globalization;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Infrastructure.Sources;

public sealed class NubeJobSourceProvider : IJobSourceProvider
{
    private readonly HttpClient _httpClient;

    public NubeJobSourceProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "Nube";
    public string Description => "Portal publico Nube consultado pela API publica de listagem e detalhes.";
    public bool IsEnabled => true;

    public async Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken)
    {
        using var response = await _httpClient.GetAsync(
            "api/portal/buscar_listagem_vagas?limite=200", cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<NubeListResponse>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);

        if (payload?.Status != 200 || payload.Param?.Dictionary is null)
            return Array.Empty<JobListingInput>();

        var jobsByUrl = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);
        var jobsByTitleCompany = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in payload.Param.Dictionary)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var view = entry.Value?.View;
            var vacancy = entry.Value?.Vacancy;
            if (view is null || string.IsNullOrWhiteSpace(view.Url))
                continue;
            if (!entry.Key.Equals(view.Id.ToString(CultureInfo.InvariantCulture), StringComparison.OrdinalIgnoreCase))
                continue;
            if (!view.Url.Contains("/detalhes-vaga/", StringComparison.OrdinalIgnoreCase))
                continue;

            var title = NormalizeTitle(view.Title, vacancy?.RegexFilter);
            var company = ExtractCompany(view.Destaque);
            var location = BuildLocation(view.City, view.State, view.Location);
            var text = $"{title} {vacancy?.RegexFilter} {view.Subtitle}";

            if (!IsRelevantTechnologyRole(text))
                continue;

            var externalId = view.Id > 0
                ? view.Id.ToString(CultureInfo.InvariantCulture)
                : entry.Key;
            var jobUrl = NormalizeJobUrl(view.Url);
            var description = await FetchDescriptionAsync(view, jobUrl, cancellationToken);
            description = AppendBenefits(description, view.Benefits);
            if (view.Inclusion == 1)
                description = $"{description}\nVaga inclusiva publicada pelo Nube.";

            var job = new JobListingInput(
                externalId,
                title,
                company,
                location,
                MapWorkMode(view.ModeId),
                NormalizeSalary(view.Salary),
                Name,
                jobUrl,
                description,
                InferCategory(text, description),
                DateTimeOffset.UtcNow);

            var urlKey = NormalizeUrl(job.Url);
            var titleCompanyKey = BuildTitleCompanyKey(job.Title, job.Company);
            if (!jobsByUrl.ContainsKey(urlKey) && !jobsByTitleCompany.ContainsKey(titleCompanyKey))
            {
                jobsByUrl[urlKey] = job;
                jobsByTitleCompany[titleCompanyKey] = job;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        return jobsByUrl.Values.ToArray();
    }

    private async Task<string> FetchDescriptionAsync(
        NubeView view,
        string jobUrl,
        CancellationToken cancellationToken)
    {
        if (view.Id <= 0)
            return $"{view.Type}. {view.Subtitle}".Trim();

        var slug = jobUrl.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();
        if (string.IsNullOrWhiteSpace(slug))
            return $"{view.Type}. {view.Subtitle}".Trim();

        try
        {
            var detailUrl = $"api/portal/buscar-detalhes-vaga/json?id_vaga={view.Id}&titulo={Uri.EscapeDataString(slug)}";
            using var response = await _httpClient.GetAsync(detailUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return BuildFallbackDescription(view);

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken);
            if (!document.RootElement.TryGetProperty("status", out var status) ||
                status.GetInt32() != 200 ||
                !document.RootElement.TryGetProperty("param", out var param))
                return BuildFallbackDescription(view);

            var pieces = new List<string>();
            AppendDetail(pieces, param, "texto_curso_destaque");
            AppendDetail(pieces, param, "_jobposting_sch");
            AppendDetail(pieces, param, "share", "sumario");

            return pieces.Count == 0
                ? BuildFallbackDescription(view)
                : StripHtml(string.Join(" ", pieces));
        }
        catch (HttpRequestException)
        {
            return BuildFallbackDescription(view);
        }
    }

    private static void AppendDetail(List<string> pieces, JsonElement param, string property)
    {
        if (param.TryGetProperty(property, out var value))
        {
            var text = value.ValueKind == JsonValueKind.String
                ? value.GetString()
                : value.ToString();
            if (!string.IsNullOrWhiteSpace(text))
                pieces.Add(text);
        }
    }

    private static void AppendDetail(List<string> pieces, JsonElement param, string parent, string property)
    {
        if (param.TryGetProperty(parent, out var parentValue) &&
            parentValue.ValueKind == JsonValueKind.Object &&
            parentValue.TryGetProperty(property, out var value))
        {
            var text = value.GetString();
            if (!string.IsNullOrWhiteSpace(text))
                pieces.Add(text);
        }
    }

    private static string BuildFallbackDescription(NubeView view)
    {
        var pieces = new[] { view.Type, view.Subtitle, view.Location }
            .Where(piece => !string.IsNullOrWhiteSpace(piece));
        return string.Join(". ", pieces);
    }

    private static string AppendBenefits(string description, IReadOnlyList<NubeBenefit>? benefits)
    {
        var names = benefits?
            .Where(benefit => benefit.Mostrar)
            .Select(benefit => benefit.Title)
            .Where(title => !string.IsNullOrWhiteSpace(title))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray();
        if (names is null || names.Length == 0) return description;
        var suffix = $"Benefícios: {string.Join(", ", names)}.";
        return string.IsNullOrWhiteSpace(description) ? suffix : $"{description} {suffix}";
    }

    private static bool IsRelevantTechnologyRole(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        string[] terms =
        [
            "tecnologia da informacao", "analise de dados", "dados", "software",
            "desenvolvimento", "programacao", "qualidade", "qa", "teste",
            "devops", "cloud", "suporte", "seguranca", "cyber", "security",
            "dados", "business intelligence", "engenharia de software",
            "sistemas", "informatica"
        ];
        return terms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }

    private static string NormalizeTitle(string? title, string? regexFilter)
    {
        var candidate = title?.Trim();
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            candidate = Regex.Replace(candidate, @"\s+-\s*\d+$", "").Trim();
            return candidate;
        }
        var filters = regexFilter?.Split('|', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Trim()).Where(part => part.Length > 0).ToArray();
        return filters?.FirstOrDefault() ?? "Vaga de tecnologia";
    }

    private static string ExtractCompany(JsonElement featured)
    {
        if (featured.ValueKind == JsonValueKind.Object &&
            featured.TryGetProperty("nome_divulgacao", out var name) &&
            name.ValueKind == JsonValueKind.String &&
            !string.IsNullOrWhiteSpace(name.GetString()))
        {
            return name.GetString()!.Trim();
        }
        return "Nube";
    }

    private static string BuildLocation(string? city, string? state, string? location)
    {
        var parts = new[] { city?.Trim(), state?.Trim() }
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
        if (parts.Length > 0) return string.Join(", ", parts);
        return string.IsNullOrWhiteSpace(location) ? "Brasil" : location.Trim();
    }

    private static WorkMode MapWorkMode(int modeId)
    {
        return modeId switch
        {
            2 => WorkMode.Remote,
            3 => WorkMode.Hybrid,
            1 => WorkMode.OnSite,
            _ => WorkMode.Unspecified
        };
    }

    private static string? NormalizeSalary(string? salary)
    {
        return string.IsNullOrWhiteSpace(salary) ? null : WebUtility.HtmlDecode(salary).Trim();
    }

    private static string NormalizeUrl(string url)
    {
        return url.TrimEnd('/').ToLowerInvariant();
    }

    private static string BuildTitleCompanyKey(string title, string company)
    {
        return $"{RemoveDiacritics(title).Trim().ToLowerInvariant()}|{RemoveDiacritics(company).Trim().ToLowerInvariant()}";
    }

    private static string NormalizeJobUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Relative, out _))
            throw new InvalidOperationException("Invalid relative Nube vacancy URL.");
        var absolute = new Uri(_httpClientUri, url);
        if (absolute.Host != "www.nube.com.br")
            throw new InvalidOperationException("Unexpected Nube vacancy host.");
        return absolute.ToString();
    }

    private static readonly Uri _httpClientUri = new("https://www.nube.com.br/");

    private static string InferCategory(string text, string description)
    {
        var combined = RemoveDiacritics($"{text} {description}".ToLowerInvariant());
        if (Contains(combined, "qa", "qualidade", "teste")) return "QA";
        if (Contains(combined, "devops", "cloud", "observabilidade")) return "DevOps";
        if (Contains(combined, "seguranca", "cyber", "security")) return "Segurança";
        if (Contains(combined, "dados", "data", "business intelligence", "sql")) return "Dados";
        if (Contains(combined, "suporte", "help desk")) return "Suporte";
        if (Contains(combined, "desenvolvimento", "software", "programacao", "informatica", "sistemas")) return "Desenvolvimento";
        return "Tecnologia da Informação";
    }

    private static bool Contains(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));

    private static string StripHtml(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return string.Empty;
        var text = Regex.Replace(WebUtility.HtmlDecode(html), "<[^>]+>", " ");
        text = Regex.Replace(text, @"\s+", " ").Trim();
        return text.Length <= 10000 ? text : text[..10000];
    }

    private static string RemoveDiacritics(string value)
    {
        var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
        var chars = normalized
            .Where(character => CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
}

    private sealed class NubeListResponse
    {
        [JsonPropertyName("status")]
        public int Status { get; init; }
        [JsonPropertyName("param")]
        public NubeParam? Param { get; init; }
    }

    private sealed class NubeParam
    {
        [JsonPropertyName("dict_por_id_vaga")]
        public Dictionary<string, NubeEntry>? Dictionary { get; init; }
    }

    private sealed class NubeEntry
    {
        [JsonPropertyName("view")]
        public NubeView? View { get; init; }
        [JsonPropertyName("vaga")]
        public NubeVacancy? Vacancy { get; init; }
    }

    private sealed class NubeVacancy
    {
        [JsonPropertyName("regex_filtro")]
        public string? RegexFilter { get; init; }
    }

    private sealed class NubeView
    {
        [JsonPropertyName("id_vaga")]
        public long Id { get; init; }
        [JsonPropertyName("titulo")]
        public string? Title { get; init; }
        [JsonPropertyName("destaque")]
        public JsonElement Destaque { get; init; }
        [JsonPropertyName("subtitulo")]
        public string? Subtitle { get; init; }
        [JsonPropertyName("tipo_vaga")]
        public string? Type { get; init; }
        [JsonPropertyName("url")]
        public string? Url { get; init; }
        [JsonPropertyName("cidade")]
        public string? City { get; init; }
        [JsonPropertyName("uf")]
        public string? State { get; init; }
        [JsonPropertyName("local")]
        public string? Location { get; init; }
        [JsonPropertyName("bolsa_valor")]
        public string? Salary { get; init; }
        [JsonPropertyName("id_modalidade_atuacao")]
        public int ModeId { get; init; }
        [JsonPropertyName("inclusao")]
        public int Inclusion { get; init; }
        [JsonPropertyName("beneficios")]
        public List<NubeBenefit>? Benefits { get; init; }
    }

    private sealed class NubeBenefit
    {
        [JsonPropertyName("mostrar")]
        public bool Mostrar { get; init; }
        [JsonPropertyName("titulo")]
        public string? Title { get; init; }
    }
}
