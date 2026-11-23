namespace JobFinder.Domain.Entities;

public sealed class UserVisitState
{
    private UserVisitState() { }

    public int Id { get; private set; }
    public DateTimeOffset LastVisitAt { get; private set; }

    public static UserVisitState Create(DateTimeOffset visitedAt) =>
        new() { Id = 1, LastVisitAt = visitedAt };

    public void MarkVisited(DateTimeOffset visitedAt) => LastVisitAt = visitedAt;
}
