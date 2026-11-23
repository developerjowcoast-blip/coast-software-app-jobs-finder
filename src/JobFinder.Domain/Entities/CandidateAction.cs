namespace JobFinder.Domain.Entities;

public sealed class CandidateAction
{
    private CandidateAction() { }

    internal CandidateAction(Guid jobListingId, string action, string? details, DateTimeOffset occurredAt)
    {
        Id = Guid.NewGuid();
        JobListingId = jobListingId;
        Action = action;
        Details = details;
        OccurredAt = occurredAt;
    }

    public Guid Id { get; private set; }
    public Guid JobListingId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    public string? Details { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }
}
