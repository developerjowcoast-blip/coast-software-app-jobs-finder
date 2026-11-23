using System.Text.Json;
using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using JobFinder.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace JobFinder.Application.Services;

public sealed class JobFinderService(
    IJobRepository repository,
    IEnumerable<IJobSourceProvider> sourceProviders,
    IJobFinderSettings settings,
    CompatibilityScorer scorer,
    Microsoft.Extensions.Logging.ILogger<JobFinderService> logger)
{
    private readonly IJobSourceProvider[] _providers = sourceProviders.ToArray();

    public async Task<JobPage> ListAsync(JobFilter filter, CancellationToken cancellationToken)
    {
        var all = await repository.GetAllAsync(cancellationToken);
        var query = all.AsEnumerable();
        if (!filter.IncludeHidden) query = query.Where(job => !job.IsHidden);
        if (filter.FavoritesOnly) query = query.Where(job => job.IsFavorite);
        if (!string.IsNullOrWhiteSpace(filter.Query))
            query = query.Where(job => $"{job.Title} {job.Company} {job.Description}".Contains(filter.Query, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Category))
            query = query.Where(job => job.Category.Contains(filter.Category, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Company))
            query = query.Where(job => job.Company.Contains(filter.Company, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrWhiteSpace(filter.Location))
            query = query.Where(job => job.Location.Contains(filter.Location, StringComparison.OrdinalIgnoreCase));
        if (filter.WorkMode.HasValue) query = query.Where(job => job.WorkMode == filter.WorkMode);
        if (filter.MinimumScore.HasValue) query = query.Where(job => job.CompatibilityScore >= Math.Clamp(filter.MinimumScore.Value, 0, 100));
        if (filter.RequiresSpanish.HasValue) query = query.Where(job => job.RequiresSpanish == filter.RequiresSpanish);
        if (filter.InclusiveEmployer.HasValue) query = query.Where(job => job.InclusiveEmployer == filter.InclusiveEmployer);
        if (filter.Status.HasValue) query = query.Where(job => job.Status == filter.Status);
        var ordered = query.OrderByDescending(job => job.CompatibilityScore).ThenByDescending(job => job.PublishedAt).ToArray();
        var page = Math.Max(1, filter.Page);
        var pageSize = Math.Clamp(filter.PageSize, 1, 200);
        return new JobPage(ordered.Skip((page - 1) * pageSize).Take(pageSize).Select(ToDto).ToArray(), page, pageSize, ordered.Length);
    }

    public async Task<JobDto?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        return job is null ? null : ToDto(job);
    }

    public async Task<IReadOnlyList<JobActionDto>?> GetActionsAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        return job?.Actions.OrderByDescending(action => action.OccurredAt)
            .Select(action => new JobActionDto(action.Id, action.Action, action.Details, action.OccurredAt)).ToArray();
    }

    public async Task<NewsPageDto> NewsAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var lastVisit = await repository.GetLastVisitAsync(cancellationToken);
        var cutoff = lastVisit ?? now.AddHours(-24);
        var jobs = await repository.GetAllAsync(cancellationToken);
        var items = jobs
            .Where(job => !job.IsHidden)
            .Where(job => job.FirstSeenAt > cutoff ||
                          job.ScoreChangedAt.HasValue && job.ScoreChangedAt.Value > cutoff)
            .OrderByDescending(job => job.ScoreChangedAt ?? job.FirstSeenAt)
            .ThenByDescending(job => job.CompatibilityScore)
            .Select(job =>
            {
                var dto = ToDto(job);
                var scoreChanged = job.ScoreChangedAt.HasValue &&
                                   job.ScoreChangedAt.Value > cutoff &&
                                   job.PreviousCompatibilityScore.HasValue;
                return new NewsJobDto(
                    dto,
                    job.FirstSeenAt > cutoff,
                    job.FirstSeenAt >= now.AddHours(-24),
                    scoreChanged,
                    job.PreviousCompatibilityScore,
                    job.ScoreChangedAt,
                    job.IsViewed,
                    job.ViewedAt);
            })
            .ToArray();
        return new NewsPageDto(items, lastVisit, now, items.Length);
    }

    public async Task<bool> MarkNewsViewedAsync(Guid id, CancellationToken cancellationToken) =>
        await repository.MarkViewedAsync(id, cancellationToken);

    public async Task<DateTimeOffset> RecordNewsVisitAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        await repository.RecordVisitAsync(now, cancellationToken);
        return now;
    }

    public async Task<int> CollectAsync(CancellationToken cancellationToken)
    {
        var inputs = new List<JobListingInput>();
        foreach (var provider in _providers.Where(provider => provider.IsEnabled && settings.EnabledSourceNames.Contains(provider.Name, StringComparer.OrdinalIgnoreCase)))
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    inputs.AddRange(await provider.FetchAsync(cancellationToken));
                    break;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
                catch when (attempt < 3)
                {
                    await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), cancellationToken);
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Source {Source} failed after {Attempts} attempts.", provider.Name, attempt);
                    break;
                }
            }
        }

        var existingJobs = await repository.GetAllAsync(cancellationToken);
        var staleGupyDemos = existingJobs
            .Where(job => job.Source.Equals("Gupy", StringComparison.OrdinalIgnoreCase)
                && (job.ExternalId.StartsWith("sample-", StringComparison.OrdinalIgnoreCase)
                    || job.Url.Contains("example.com", StringComparison.OrdinalIgnoreCase)))
            .Select(job => job.Id)
            .ToArray();
        var staleNubeDemos = existingJobs
            .Where(job => job.Source.Equals("Nube", StringComparison.OrdinalIgnoreCase)
                && (job.ExternalId.StartsWith("sample-", StringComparison.OrdinalIgnoreCase)
                    || job.Url.Contains("example.com", StringComparison.OrdinalIgnoreCase)
                    || !job.Url.Contains("/detalhes-vaga/", StringComparison.OrdinalIgnoreCase)))
            .Select(job => job.Id)
            .ToArray();
        var staleCieeDemos = existingJobs
            .Where(job => job.Source.Equals("CIEE", StringComparison.OrdinalIgnoreCase)
                && (job.ExternalId.StartsWith("sample-", StringComparison.OrdinalIgnoreCase)
                    || job.Url.Contains("example.com", StringComparison.OrdinalIgnoreCase)))
            .Select(job => job.Id)
            .ToArray();
        var staleTramposDemos = existingJobs
            .Where(job => job.Source.Equals("Trampos", StringComparison.OrdinalIgnoreCase)
                && (job.ExternalId.StartsWith("sample-", StringComparison.OrdinalIgnoreCase)
                    || job.Url.Contains("example.com", StringComparison.OrdinalIgnoreCase)))
            .Select(job => job.Id)
            .ToArray();
        var staleVagasDemos = existingJobs
            .Where(job => job.Source.Equals("Vagas.com.br", StringComparison.OrdinalIgnoreCase)
                && (job.ExternalId.StartsWith("sample-", StringComparison.OrdinalIgnoreCase)
                    || job.Url.Contains("example.com", StringComparison.OrdinalIgnoreCase)))
            .Select(job => job.Id)
            .ToArray();
        var staleVagasIds = staleVagasDemos
            .Concat(existingJobs
                .Where(job => job.Source.Equals("Vagas.com.br", StringComparison.OrdinalIgnoreCase))
                .GroupBy(job => BuildTitleCompanyKey(job.Title, job.Company), StringComparer.OrdinalIgnoreCase)
                .SelectMany(group => group
                    .OrderByDescending(job => job.FirstSeenAt)
                    .ThenByDescending(job => job.IsFavorite)
                    .Skip(1)
                    .Select(job => job.Id)))
            .Distinct()
            .ToArray();
        var staleTramposIds = staleTramposDemos
            .Concat(existingJobs
                .Where(job => job.Source.Equals("Trampos", StringComparison.OrdinalIgnoreCase))
                .GroupBy(job => BuildTitleCompanyKey(job.Title, job.Company), StringComparer.OrdinalIgnoreCase)
                .SelectMany(group => group
                    .OrderByDescending(job => job.FirstSeenAt)
                    .ThenByDescending(job => job.IsFavorite)
                    .Skip(1)
                    .Select(job => job.Id)))
            .Distinct()
            .ToArray();
        var staleCieeIds = staleCieeDemos
            .Concat(existingJobs
                .Where(job => job.Source.Equals("CIEE", StringComparison.OrdinalIgnoreCase))
                .GroupBy(job => BuildTitleCompanyKey(job.Title, job.Company), StringComparer.OrdinalIgnoreCase)
                .SelectMany(group => group
                    .OrderByDescending(job => job.FirstSeenAt)
                    .ThenByDescending(job => job.IsFavorite)
                    .Skip(1)
                    .Select(job => job.Id)))
            .Distinct()
            .ToArray();
        var staleNubeIds = staleNubeDemos
            .Concat(existingJobs
                .Where(job => job.Source.Equals("Nube", StringComparison.OrdinalIgnoreCase))
                .GroupBy(job => BuildTitleCompanyKey(job.Title, job.Company), StringComparer.OrdinalIgnoreCase)
                .SelectMany(group => group
                    .OrderByDescending(job => job.FirstSeenAt)
                    .ThenByDescending(job => job.IsFavorite)
                    .Skip(1)
                    .Select(job => job.Id)))
            .Distinct()
            .ToArray();
        await repository.RemoveManyAsync(
            staleGupyDemos.Concat(staleCieeIds).Concat(staleNubeIds).Concat(staleTramposIds).Concat(staleVagasIds).Distinct().ToArray(),
            cancellationToken);

        var jobs = inputs
            .GroupBy(input => $"{input.Source}:{input.ExternalId}", StringComparer.OrdinalIgnoreCase)
            .Select(group => group.Last())
            .DistinctBy(input => NormalizeUrl(input.Url), StringComparer.OrdinalIgnoreCase)
            .DistinctBy(input => BuildTitleCompanyKey(input.Title, input.Company), StringComparer.OrdinalIgnoreCase)
            .Select(input =>
            {
                var listing = JobListing.Create(input.ExternalId, input.Title, input.Company, input.Location, input.WorkMode,
                    input.Salary, input.Source, input.Url, input.Description, input.Category, input.PublishedAt);
                var result = scorer.Score(input);
                listing.SetCompatibility(result.Score, result.Summary,
                    CompatibilityScorer.SerializeReasons(result.Reasons), result.RequiresSpanish, result.Inclusive);
                return listing;
            }).ToArray();
        await repository.UpsertManyAsync(jobs, settings.Current.MaximumRecords, cancellationToken);
        return jobs.Length;
    }

    private static string NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return url.Trim().TrimEnd('/').ToLowerInvariant();
        var builder = new UriBuilder(uri) { Fragment = string.Empty, Query = string.Empty };
        return builder.Uri.ToString().TrimEnd('/').ToLowerInvariant();
    }

    private static string BuildTitleCompanyKey(string title, string company)
    {
        static string Normalize(string value)
        {
            var normalized = value.Normalize(System.Text.NormalizationForm.FormD);
            var chars = normalized.Where(character =>
                System.Globalization.CharUnicodeInfo.GetUnicodeCategory(character) !=
                System.Globalization.UnicodeCategory.NonSpacingMark);
            return new string(chars.ToArray()).Trim().ToLowerInvariant();
        }
        return $"{Normalize(title)}|{Normalize(company)}";
    }

    public async Task<bool> SetFavoriteAsync(Guid id, bool favorite, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null) return false;
        job.SetFavorite(favorite);
        job.RecordAction(favorite ? "favorited" : "unfavorited");
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetStatusAsync(Guid id, ApplicationStatus status, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null) return false;
        job.SetStatus(status);
        job.RecordAction("status_changed", status.ToString());
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetHiddenAsync(Guid id, bool hidden, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null) return false;
        job.SetHidden(hidden);
        job.RecordAction(hidden ? "hidden" : "unhidden");
        await repository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> RecordOpenAsync(Guid id, CancellationToken cancellationToken)
    {
        var job = await repository.GetByIdAsync(id, cancellationToken);
        if (job is null) return null;
        job.RecordAction("opened");
        await repository.SaveChangesAsync(cancellationToken);
        return job.Url;
    }

    public async Task<DashboardDto> DashboardAsync(CancellationToken cancellationToken)
    {
        var jobs = await repository.GetAllAsync(cancellationToken);
        return new DashboardDto(jobs.Count, jobs.Count(job => job.FirstSeenAt.UtcDateTime.Date == DateTime.UtcNow.Date),
            jobs.Count(job => job.IsFavorite),
            jobs.Count(job => job.Status is ApplicationStatus.Applied or ApplicationStatus.InReview or ApplicationStatus.Interview or ApplicationStatus.Offer),
            jobs.Count == 0 ? 0 : Math.Round(jobs.Average(job => job.CompatibilityScore), 1),
            settings.Sources.Count(source => source.Enabled), jobs.Count == 0 ? null : jobs.Max(job => job.LastUpdatedAt));
    }

    public async Task<StatisticsDto> StatisticsAsync(CancellationToken cancellationToken)
    {
        var jobs = await repository.GetAllAsync(cancellationToken);
        static IReadOnlyDictionary<string, int> Count(IEnumerable<IGrouping<string, JobListing>> groups) =>
            groups.ToDictionary(group => group.Key, group => group.Count());
        return new StatisticsDto(
            Count(jobs.GroupBy(job => job.Source)),
            Count(jobs.GroupBy(job => string.IsNullOrWhiteSpace(job.Category) ? "Não informado" : job.Category)),
            Count(jobs.GroupBy(job => job.WorkMode.ToString())),
            Count(jobs.GroupBy(job => job.Company).OrderByDescending(group => group.Count()).Take(10)),
            Count(jobs.GroupBy(job => job.PublishedAt.ToString("yyyy-MM"))));
    }

    public IReadOnlyList<SourceDto> Sources() => settings.Sources;
    public AppSettingsDto Settings() => settings.Current;
    public void UpdateSettings(SettingsUpdate update) => settings.Update(update);
    public void SetSourceEnabled(string name, bool enabled) => settings.SetSourceEnabled(name, enabled);

    private static JobDto ToDto(JobListing job) => new(
        job.Id, job.Title, job.Company, job.Location, job.WorkMode, job.Salary, job.Source, job.Url,
        job.PublishedAt, job.FirstSeenAt, job.CompatibilityScore,
        CompatibilityScorer.DeserializeReasons(job.CompatibilityReasonsJson), job.Summary, job.Category,
        job.RequiresSpanish, job.InclusiveEmployer, job.IsFavorite, job.IsHidden, job.Status);
}
