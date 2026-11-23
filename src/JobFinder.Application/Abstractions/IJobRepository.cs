using JobFinder.Domain.Entities;

namespace JobFinder.Application.Abstractions;

public interface IJobRepository
{
    Task<IReadOnlyList<JobListing>> GetAllAsync(CancellationToken cancellationToken);
    Task<JobListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpsertManyAsync(IReadOnlyCollection<JobListing> jobs, int maximumRecords, CancellationToken cancellationToken);
    Task RemoveManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken);
    Task<bool> MarkViewedAsync(Guid id, CancellationToken cancellationToken);
    Task<DateTimeOffset?> GetLastVisitAsync(CancellationToken cancellationToken);
    Task RecordVisitAsync(DateTimeOffset visitedAt, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}
