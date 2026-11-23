using JobFinder.Domain.Entities;

namespace JobFinder.Application.Contracts;

public sealed record JobListingInput(
    string ExternalId, string Title, string Company, string Location, WorkMode WorkMode,
    string? Salary, string Source, string Url, string Description, string Category, DateTimeOffset PublishedAt);

public sealed record JobFilter(
    string? Query = null, string? Category = null, WorkMode? WorkMode = null,
    string? Company = null, string? Location = null, int? MinimumScore = null,
    bool? RequiresSpanish = null, bool? InclusiveEmployer = null, bool FavoritesOnly = false,
    bool IncludeHidden = false, int Page = 1, int PageSize = 50, ApplicationStatus? Status = null);

public sealed record JobDto(
    Guid Id, string Title, string Company, string Location, WorkMode WorkMode, string? Salary,
    string Source, string Url, DateTimeOffset PublishedAt, DateTimeOffset FirstSeenAt,
    int CompatibilityScore, IReadOnlyList<string> CompatibilityReasons, string Summary,
    string Category, bool RequiresSpanish, bool InclusiveEmployer, bool IsFavorite,
    bool IsHidden, ApplicationStatus Status);

public sealed record DashboardDto(
    int TotalJobs, int NewJobsToday, int Favorites, int ActiveApplications,
    double AverageCompatibility, int MonitoredSources, DateTimeOffset? LastUpdatedAt);

public sealed record SourceDto(string Name, bool Enabled, string Description, DateTimeOffset? LastCheckedAt);
public sealed record JobActionDto(Guid Id, string Action, string? Details, DateTimeOffset OccurredAt);
public sealed record AppSettingsDto(int RefreshIntervalMinutes, int MaximumRecords, string Theme, string Database);
public sealed record SettingsUpdate(int? RefreshIntervalMinutes, int? MaximumRecords, string? Theme);
public sealed record StatisticsDto(
    IReadOnlyDictionary<string, int> BySource, IReadOnlyDictionary<string, int> ByCategory,
    IReadOnlyDictionary<string, int> ByWorkMode, IReadOnlyDictionary<string, int> ByCompany,
    IReadOnlyDictionary<string, int> Monthly);
public sealed record JobPage(IReadOnlyList<JobDto> Items, int Page, int PageSize, int Total);
public sealed record NewsJobDto(
    JobDto Job, bool IsNewSinceLastVisit, bool CollectedLast24Hours,
    bool ScoreChanged, int? PreviousScore, DateTimeOffset? ScoreChangedAt,
    bool IsViewed, DateTimeOffset? ViewedAt);
public sealed record NewsPageDto(
    IReadOnlyList<NewsJobDto> Items, DateTimeOffset? LastVisitAt,
    DateTimeOffset CurrentVisitAt, int Total);
