using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Infrastructure.Sources;

public sealed class CieeJobSourceProvider : IJobSourceProvider
{
    private readonly HttpClient _httpClient;

    public CieeJobSourceProvider(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    public string Name => "CIEE";
    public string Description => "Vitrine publica CIEE de processos seletivos consultada pela API publica.";
    public bool IsEnabled => true;

    public async Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "api/editais/vitrine?page=0&size=30");
        request.Content = new StringContent("{}", Encoding.UTF8, "application/json");
        using var response = await _httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var payload = await JsonSerializer.DeserializeAsync<CieePage>(stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);

        var jobsByUrl = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);
        var jobsByTitleCompany = new Dictionary<string, JobListingInput>(StringComparer.OrdinalIgnoreCase);

        foreach (var edital in payload?.Content ?? [])
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!edital.Ativo || !edital.Status.Equals("ABERTO", StringComparison.OrdinalIgnoreCase))
                continue;

            CieeDetail? detail = null;
            try
            {
                using var detailResponse = await _httpClient.GetAsync(
                    $"api/editais/{edital.Id}", cancellationToken);
                if (detailResponse.IsSuccessStatusCode)
                {
                    await using var detailStream = await detailResponse.Content.ReadAsStreamAsync(cancellationToken);
                    detail = await JsonSerializer.DeserializeAsync<CieeDetail>(detailStream,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true }, cancellationToken);
                }
            }
            catch (HttpRequestException)
            {
            }

            var courses = (detail?.Courses ?? [])
                .Select(course => course.Course?.Description)
                .OfType<string>()
                .Where(description => !string.IsNullOrWhiteSpace(description))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray() ?? Array.Empty<string>();
            var title = BuildTitle(edital.Identificacao, courses);
            var company = detail?.Orgao?.Nome ?? edital.Orgao?.Nome ?? "CIEE";
            var location = BuildLocation(detail);
            var description = BuildDescription(detail, courses);

            if (!IsRelevantTechnologyRole($"{title} {string.Join(" ", courses)}"))
                continue;

            var jobUrl = $"https://pp.ciee.org.br/vitrine/{edital.Id}/detalhe";
            var job = new JobListingInput(
                edital.Id.ToString(CultureInfo.InvariantCulture),
                title,
                company,
                location,
                WorkMode.OnSite,
                ExtractSalary(detail),
                Name,
                jobUrl,
                description,
                InferCategory($"{title} {string.Join(" ", courses)}", description),
                ParsePublishedAt(detail?.Cronogramas));

            var urlKey = jobUrl.TrimEnd('/').ToLowerInvariant();
            var titleCompanyKey = BuildTitleCompanyKey(title, company);
            if (!jobsByUrl.ContainsKey(urlKey) && !jobsByTitleCompany.ContainsKey(titleCompanyKey))
            {
                jobsByUrl[urlKey] = job;
                jobsByTitleCompany[titleCompanyKey] = job;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50), cancellationToken);
        }

        return jobsByUrl.Values.ToArray();
    }

    private static string BuildTitle(string? identification, string[] courses)
    {
        if (courses.Length > 0)
            return $"Processo CIEE - {Regex.Replace(courses[0].Trim(), @"\s+", " ")}";
        return string.IsNullOrWhiteSpace(identification)
            ? "Processo seletivo CIEE"
            : $"Processo CIEE - {Regex.Replace(identification.Trim(), @"\s+", " ")}";
    }

    private static string BuildLocation(CieeDetail? detail)
    {
        var locations = (detail?.LocationLinks ?? [])
            .SelectMany(link => link.Locations ?? [])
            .Select(location => location.City)
            .OfType<string>()
            .Where(city => !string.IsNullOrWhiteSpace(city))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray() ?? [];
        if (locations.Length > 0)
            return locations.Length == 1 ? locations[0]! : string.Join(", ", locations);

        var directLocations = (detail?.Courses ?? [])
            .SelectMany(course => course.Locations ?? [])
            .Select(location => location.City)
            .OfType<string>()
            .Where(city => !string.IsNullOrWhiteSpace(city))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToArray() ?? [];
        return directLocations.Length == 0 ? "Brasil" : string.Join(", ", directLocations);
    }

    private static string BuildDescription(CieeDetail? detail, string[] courses)
    {
        var pieces = new List<string>();
        if (courses.Length > 0)
            pieces.Add($"Cursos relacionados: {string.Join(", ", courses)}.");
        if (!string.IsNullOrWhiteSpace(detail?.Requisitos?.Description))
            pieces.Add(StripHtml(detail.Requisitos.Description));
        if (!string.IsNullOrWhiteSpace(detail?.Abrangencia))
            pieces.Add($"Abrangência: {detail.Abrangencia}.");
        if (detail?.Jornadas is { Count: > 0 })
            pieces.Add($"Jornada: {string.Join(", ", detail.Jornadas.Select(jornada => jornada.Description).Where(value => !string.IsNullOrWhiteSpace(value)))}.");
        return pieces.Count == 0
            ? "Processo seletivo público CIEE para estágio ou aprendizagem."
            : string.Join(" ", pieces);
    }

    private static string? ExtractSalary(CieeDetail? detail)
    {
        var html = detail?.Requisitos?.Description;
        if (string.IsNullOrWhiteSpace(html)) return null;
        var match = Regex.Match(html, @"R\$\s*[0-9.,]+", RegexOptions.IgnoreCase);
        return match.Success ? match.Value.Replace(" ", "") : null;
    }

    private static DateTimeOffset ParsePublishedAt(IReadOnlyList<CieeSchedule>? schedules)
    {
        foreach (var schedule in schedules ?? [])
        {
            if (schedule.Type == "INSCRICAO" &&
                DateTimeOffset.TryParse(schedule.Start, CultureInfo.InvariantCulture,
                    DateTimeStyles.AllowWhiteSpaces | DateTimeStyles.AssumeUniversal, out var start))
                return start.ToUniversalTime();
        }
        return DateTimeOffset.UtcNow;
    }

    private static bool IsRelevantTechnologyRole(string text)
    {
        var normalized = RemoveDiacritics(text).ToLowerInvariant();
        string[] terms =
        [
            "tecnologia", "informatica", "sistemas", "software", "dados",
            "analise", "programacao", "desenvolvimento", "qualidade", "teste",
            "qa", "devops", "cloud", "cyber", "seguranca", "suporte",
            "ti", "engenharia de software", "computacao"
        ];
        return terms.Any(term => normalized.Contains(term, StringComparison.Ordinal));
    }

    private static string InferCategory(string text, string description)
    {
        var combined = RemoveDiacritics($"{text} {description}".ToLowerInvariant());
        if (Contains(combined, "qa", "qualidade", "teste")) return "QA";
        if (Contains(combined, "devops", "cloud", "observabilidade")) return "DevOps";
        if (Contains(combined, "seguranca", "cyber", "security")) return "Segurança";
        if (Contains(combined, "desenvolvimento", "software", "programacao", "sistemas", "computacao")) return "Desenvolvimento";
        if (Contains(combined, "dados", "data", "business intelligence", "sql")) return "Dados";
        if (Contains(combined, "suporte", "help desk")) return "Suporte";
        return "Tecnologia da Informação";
    }

    private static bool Contains(string text, params string[] terms) =>
        terms.Any(term => text.Contains(term, StringComparison.Ordinal));

    private static string BuildTitleCompanyKey(string title, string company)
    {
        return $"{RemoveDiacritics(title).Trim().ToLowerInvariant()}|{RemoveDiacritics(company).Trim().ToLowerInvariant()}";
    }

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
        var chars = normalized.Where(character =>
            CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark);
        return new string(chars.ToArray()).Normalize(System.Text.NormalizationForm.FormC);
    }

    private sealed class CieePage
    {
        [JsonPropertyName("content")]
        public List<CieeEdital>? Content { get; init; }
    }

    private sealed class CieeEdital
    {
        [JsonPropertyName("id")]
        public long Id { get; init; }
        [JsonPropertyName("identificacao")]
        public string? Identificacao { get; init; }
        [JsonPropertyName("ativo")]
        public bool Ativo { get; init; }
        [JsonPropertyName("status")]
        public string Status { get; init; } = string.Empty;
        [JsonPropertyName("orgao")]
        public CieeOrgao? Orgao { get; init; }
    }

    private sealed class CieeDetail
    {
        [JsonPropertyName("orgao")]
        public CieeOrgao? Orgao { get; init; }
        [JsonPropertyName("abrangencia")]
        public string? Abrangencia { get; init; }
        [JsonPropertyName("requisitos")]
        public CieeRequirements? Requisitos { get; init; }
        [JsonPropertyName("cronogramas")]
        public List<CieeSchedule>? Cronogramas { get; init; }
        [JsonPropertyName("jornadas")]
        public List<CieeJourney>? Jornadas { get; init; }
        [JsonPropertyName("editalCursos")]
        public List<CieeCourse>? Courses { get; init; } = [];
        [JsonPropertyName("vinculoLocaisCursos")]
        public List<CieeLocationLink>? LocationLinks { get; init; } = [];
    }

    private sealed class CieeOrgao
    {
        [JsonPropertyName("nome")]
        public string? Nome { get; init; }
    }

    private sealed class CieeRequirements
    {
        [JsonPropertyName("descricaoRequisito")]
        public string? Description { get; init; }
    }

    private sealed class CieeSchedule
    {
        [JsonPropertyName("tipoFase")]
        public string? Type { get; init; }
        [JsonPropertyName("dataInicio")]
        public string? Start { get; init; }
    }

    private sealed class CieeJourney
    {
        [JsonPropertyName("descricao")]
        public string? Description { get; init; }
    }

    private sealed class CieeCourse
    {
        [JsonPropertyName("curso")]
        public CieeCourseInfo? Course { get; init; }
        [JsonPropertyName("listLocais")]
        public List<CieeLocation>? Locations { get; init; } = [];
    }

    private sealed class CieeCourseInfo
    {
        [JsonPropertyName("descricao")]
        public string? Description { get; init; }
    }

    private sealed class CieeLocationLink
    {
        [JsonPropertyName("listLocais")]
        public List<CieeLocation>? Locations { get; init; } = [];
    }

    private sealed class CieeLocation
    {
        [JsonPropertyName("cidade")]
        public string? City { get; init; }
        [JsonPropertyName("uf")]
        public string? State { get; init; }
    }
}
