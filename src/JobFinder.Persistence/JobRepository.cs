using JobFinder.Application.Abstractions;
using JobFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace JobFinder.Persistence;

public sealed class JobRepository(JobFinderDbContext dbContext) : IJobRepository
{
    public async Task<IReadOnlyList<JobListing>> GetAllAsync(CancellationToken cancellationToken) =>
        await dbContext.Jobs.AsNoTracking().ToListAsync(cancellationToken);

    public Task<JobListing?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        dbContext.Jobs.Include(job => job.Actions).SingleOrDefaultAsync(job => job.Id == id, cancellationToken);

    public async Task UpsertManyAsync(IReadOnlyCollection<JobListing> jobs, int maximumRecords, CancellationToken cancellationToken)
    {
        foreach (var incoming in jobs)
        {
            var existing = await dbContext.Jobs
                .Include(job => job.Actions)
                .SingleOrDefaultAsync(job => job.Source == incoming.Source && job.ExternalId == incoming.ExternalId, cancellationToken);
            if (existing is null) dbContext.Jobs.Add(incoming);
            else
            {
                existing.UpdateFrom(incoming);
                var existingReasons = incoming.CompatibilityReasonsJson;
                existing.SetCompatibility(incoming.CompatibilityScore, incoming.Summary, existingReasons,
                    incoming.RequiresSpanish, incoming.InclusiveEmployer);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        var count = await dbContext.Jobs.CountAsync(cancellationToken);
        if (count > maximumRecords)
        {
            var removeIds = await dbContext.Jobs
                .OrderBy(job => job.IsFavorite)
                .ThenBy(job => job.FirstSeenAt)
                .Take(count - maximumRecords)
                .Select(job => job.Id)
                .ToListAsync(cancellationToken);
            await dbContext.Jobs.Where(job => removeIds.Contains(job.Id)).ExecuteDeleteAsync(cancellationToken);
        }
    }

    public async Task RemoveManyAsync(IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0) return;
        await dbContext.Jobs
            .Where(job => ids.Contains(job.Id))
            .ExecuteDeleteAsync(cancellationToken);
    }

    public async Task<bool> MarkViewedAsync(Guid id, CancellationToken cancellationToken)
    {
        var viewedAt = DateTimeOffset.UtcNow;
        var affected = await dbContext.Jobs
            .Where(entity => entity.Id == id)
            .ExecuteUpdateAsync(updates => updates
                .SetProperty(entity => entity.IsViewed, true)
                .SetProperty(entity => entity.ViewedAt, viewedAt),
                cancellationToken);
        if (affected == 1)
        {
            var job = await dbContext.Jobs
                .Include(entity => entity.Actions)
                .SingleOrDefaultAsync(entity => entity.Id == id, cancellationToken);
            job?.MarkViewed();
        }
        return affected == 1;
    }

    public async Task<DateTimeOffset?> GetLastVisitAsync(CancellationToken cancellationToken)
    {
        var state = await dbContext.UserVisitStates
            .AsNoTracking()
            .SingleOrDefaultAsync(entity => entity.Id == 1, cancellationToken);
        return state?.LastVisitAt;
    }

    public async Task RecordVisitAsync(DateTimeOffset visitedAt, CancellationToken cancellationToken)
    {
        var state = await dbContext.UserVisitStates
            .SingleOrDefaultAsync(entity => entity.Id == 1, cancellationToken);
        if (state is null)
            dbContext.UserVisitStates.Add(UserVisitState.Create(visitedAt));
        else
            state.MarkVisited(visitedAt);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        // Client-generated action IDs can be treated as existing entities when first tracked through a navigation.
        foreach (var action in dbContext.ChangeTracker.Entries<CandidateAction>()
                     .Where(entry => entry.State == EntityState.Modified))
            action.State = EntityState.Added;
        return dbContext.SaveChangesAsync(cancellationToken);
    }
}
