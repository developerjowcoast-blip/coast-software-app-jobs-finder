using System.Text.Json;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;

namespace JobFinder.Application.Services;

public sealed class CompatibilityScorer
{
    private static readonly string[] Areas =
    [
        "qa", "quality assurance", "test", "software", "desenvolvimento", "developer", "devops",
        "devsecops", "sre", "cloud", "suporte", "cyber", "security", "segurança", "dados", "data",
        "business intelligence", "tecnologia", "ti"
    ];
    private static readonly string[] SpanishTerms =
        ["espanhol", "spanish", "español", "latam", "américa latina", "regional support", "international team", "global team"];
    private static readonly string[] InclusionTerms =
        ["inclus", "neurodivers", "women in tech", "mulheres na tecnologia", "diversidade"];

    public (int Score, string Summary, IReadOnlyList<string> Reasons, bool RequiresSpanish, bool Inclusive) Score(JobListingInput job)
    {
        var text = $"{job.Title} {job.Description} {job.Category}".ToLowerInvariant();
        var reasons = new List<string>();
        var score = 0;
        if (text.Contains("ads") || text.Contains("análise e desenvolvimento") || text.Contains("sistemas") ||
            text.Contains("software") || text.Contains("technology") || text.Contains("tecnologia"))
        {
            score += 30;
            reasons.Add("Área compatível com Análise e Desenvolvimento de Sistemas");
        }

        if (text.Contains("estágio") || text.Contains("estagio") || text.Contains("intern") || text.Contains("trainee") ||
            text.Contains("junior") || text.Contains("júnior") || text.Contains("entry level"))
        {
            score += 20;
            reasons.Add("Oportunidade de estágio ou início de carreira");
        }

        if (Areas.Any(text.Contains))
        {
            score += 20;
            reasons.Add($"Área de interesse: {job.Category}");
        }

        if (job.WorkMode is WorkMode.Remote or WorkMode.Hybrid ||
            job.Location.Contains("São Paulo", StringComparison.OrdinalIgnoreCase) ||
            job.Location.Contains("Sao Paulo", StringComparison.OrdinalIgnoreCase) ||
            job.Location.Contains("Brasil", StringComparison.OrdinalIgnoreCase))
        {
            score += 10;
            reasons.Add(job.WorkMode switch
            {
                WorkMode.Remote => "Modalidade remota",
                WorkMode.Hybrid => "Modalidade híbrida",
                _ => "Localização compatível com São Paulo"
            });
        }

        var requiresSpanish = SpanishTerms.Any(text.Contains);
        if (requiresSpanish)
        {
            score += 10;
            reasons.Add("Espanhol ou contexto internacional valorizado");
        }

        var inclusive = InclusionTerms.Any(text.Contains);
        if (inclusive)
        {
            score += 10;
            reasons.Add("Empresa ou programa menciona inclusão e diversidade");
        }

        if (!text.Contains("experiência obrigatória") && !text.Contains("3 anos de experiência"))
            reasons.Add("Não foi identificada exigência explícita de experiência formal");

        var summary = job.Description.Length <= 240
            ? job.Description
            : job.Description[..237].TrimEnd() + "...";
        if (string.IsNullOrWhiteSpace(summary)) summary = $"{job.Title} na {job.Company}. Consulte o anúncio original para detalhes.";
        return (Math.Clamp(score, 0, 100), summary, reasons, requiresSpanish, inclusive);
    }

    public static string SerializeReasons(IEnumerable<string> reasons) => JsonSerializer.Serialize(reasons);
    public static IReadOnlyList<string> DeserializeReasons(string? json)
    {
        try { return JsonSerializer.Deserialize<List<string>>(json ?? "[]") ?? []; }
        catch (JsonException) { return []; }
    }
}
