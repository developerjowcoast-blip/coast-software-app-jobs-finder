namespace JobFinder.Domain.Entities;

public enum ApplicationStatus
{
    Unseen,
    Interesting,
    Favorited,
    Applied,
    InReview,
    Interview,
    Offer,
    Rejected,
    Closed
}

public enum WorkMode
{
    Unspecified,
    Remote,
    Hybrid,
    OnSite
}

public sealed class JobListing
{
    private JobListing() { }

    public Guid Id { get; private set; }
    public string ExternalId { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public string Company { get; private set; } = string.Empty;
    public string Location { get; private set; } = string.Empty;
    public WorkMode WorkMode { get; private set; }
    public string? Salary { get; private set; }
    public string Source { get; private set; } = string.Empty;
    public string Url { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public string Summary { get; private set; } = string.Empty;
    public string Category { get; private set; } = string.Empty;
    public DateTimeOffset PublishedAt { get; private set; }
    public DateTimeOffset FirstSeenAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public int CompatibilityScore { get; private set; }
    public string CompatibilityReasonsJson { get; private set; } = "[]";
    public bool RequiresSpanish { get; private set; }
    public bool InclusiveEmployer { get; private set; }
    public bool IsFavorite { get; private set; }
    public bool IsHidden { get; private set; }
    public bool IsViewed { get; private set; }
    public DateTimeOffset? ViewedAt { get; private set; }
    public int? PreviousCompatibilityScore { get; private set; }
    public DateTimeOffset? ScoreChangedAt { get; private set; }
    public ApplicationStatus Status { get; private set; }
    public ICollection<CandidateAction> Actions { get; private set; } = new List<CandidateAction>();

    public static JobListing Create(
        string externalId, string title, string company, string location, WorkMode workMode,
        string? salary, string source, string url, string description, string category,
        DateTimeOffset publishedAt)
    {
        if (string.IsNullOrWhiteSpace(externalId)) throw new ArgumentException("External ID is required.", nameof(externalId));
        if (string.IsNullOrWhiteSpace(title)) throw new ArgumentException("Title is required.", nameof(title));
        if (string.IsNullOrWhiteSpace(company)) throw new ArgumentException("Company is required.", nameof(company));
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https"))
            throw new ArgumentException("A valid HTTP(S) job URL is required.", nameof(url));

        var now = DateTimeOffset.UtcNow;
        return new JobListing
        {
            Id = Guid.NewGuid(),
            ExternalId = externalId.Trim(),
            Title = title.Trim(),
            Company = company.Trim(),
            Location = location?.Trim() ?? string.Empty,
            WorkMode = workMode,
            Salary = salary?.Trim(),
            Source = source.Trim(),
            Url = uri.ToString(),
            Description = description?.Trim() ?? string.Empty,
            Category = category?.Trim() ?? string.Empty,
            PublishedAt = publishedAt,
            FirstSeenAt = now,
            LastUpdatedAt = now,
            Status = ApplicationStatus.Unseen
        };
    }

    public void UpdateFrom(JobListing incoming)
    {
        Title = incoming.Title;
        Company = incoming.Company;
        Location = incoming.Location;
        WorkMode = incoming.WorkMode;
        Salary = incoming.Salary;
        Source = incoming.Source;
        Url = incoming.Url;
        Description = incoming.Description;
        Category = incoming.Category;
        PublishedAt = incoming.PublishedAt;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    public void SetCompatibility(int score, string summary, string reasonsJson, bool requiresSpanish, bool inclusiveEmployer)
    {
        var normalizedScore = Math.Clamp(score, 0, 100);
        if (CompatibilityScore != 0 && CompatibilityScore != normalizedScore)
        {
            PreviousCompatibilityScore = CompatibilityScore;
            ScoreChangedAt = DateTimeOffset.UtcNow;
        }
        else if (CompatibilityScore == 0 && normalizedScore != 0)
        {
            ScoreChangedAt = null;
            PreviousCompatibilityScore = null;
        }
        CompatibilityScore = normalizedScore;
        Summary = summary;
        CompatibilityReasonsJson = reasonsJson;
        RequiresSpanish = requiresSpanish;
        InclusiveEmployer = inclusiveEmployer;
    }

    public void SetFavorite(bool value)
    {
        IsFavorite = value;
        if (value) Status = ApplicationStatus.Favorited;
        else if (Status == ApplicationStatus.Favorited) Status = ApplicationStatus.Interesting;
    }

    public void SetStatus(ApplicationStatus value)
    {
        Status = value;
        IsFavorite = value == ApplicationStatus.Favorited || IsFavorite && value != ApplicationStatus.Rejected && value != ApplicationStatus.Closed;
    }

    public void SetHidden(bool value) => IsHidden = value;
    public void MarkViewed()
    {
        IsViewed = true;
        ViewedAt = DateTimeOffset.UtcNow;
        RecordAction("viewed");
    }

    public void RecordAction(string action, string? details = null)
    {
        Actions.Add(new CandidateAction(Id, action, details, DateTimeOffset.UtcNow));
    }
}
