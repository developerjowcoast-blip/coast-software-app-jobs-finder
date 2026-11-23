using JobFinder.Domain.Entities;
using JobFinder.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace JobFinder.Tests;

public sealed class PersistenceIntegrationTests
{
    [Fact]
    public async Task VisitAndViewedStateArePersistedInSqlite()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<JobFinderDbContext>().UseSqlite(connection).Options;
        await using var database = new JobFinderDbContext(options);
        await database.Database.EnsureCreatedAsync();
        var repository = new JobRepository(database);
        var job = JobListing.Create("news-1", "Estágio QA", "Empresa", "São Paulo",
            WorkMode.Hybrid, null, "Gupy", "https://empresa.gupy.io/job/news-1",
            "Descrição QA", "QA", DateTimeOffset.UtcNow);
        await repository.UpsertManyAsync([job], 100, CancellationToken.None);

        var visit = new DateTimeOffset(2026, 11, 20, 12, 0, 0, TimeSpan.Zero);
        await repository.RecordVisitAsync(visit, CancellationToken.None);
        var marked = await repository.MarkViewedAsync(job.Id, CancellationToken.None);

        Assert.True(marked);
        Assert.Equal(visit, await repository.GetLastVisitAsync(CancellationToken.None));
        var saved = await repository.GetByIdAsync(job.Id, CancellationToken.None);
        Assert.True(saved!.IsViewed);
        Assert.NotNull(saved.ViewedAt);
    }

    [Fact]
    public async Task RemoveManyDeletesSelectedListing()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<JobFinderDbContext>().UseSqlite(connection).Options;
        await using var database = new JobFinderDbContext(options);
        await database.Database.EnsureCreatedAsync();
        var repository = new JobRepository(database);
        var demo = JobListing.Create("sample-gupy", "Old Gupy demo", "Example", "São Paulo",
            WorkMode.Hybrid, null, "Gupy", "https://example.com/jobs/gupy", "Demo", "QA",
            DateTimeOffset.UtcNow);
        await repository.UpsertManyAsync([demo], 100, CancellationToken.None);

        await repository.RemoveManyAsync([demo.Id], CancellationToken.None);

        Assert.Empty(await repository.GetAllAsync(CancellationToken.None));
    }

    [Fact]
    public async Task UpsertUpdatesExistingListingWithoutLosingCandidateStateOrHistory()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = new DbContextOptionsBuilder<JobFinderDbContext>().UseSqlite(connection).Options;
        await using var database = new JobFinderDbContext(options);
        await database.Database.EnsureCreatedAsync();
        var repository = new JobRepository(database);

        var initial = JobListing.Create("external-1", "QA Intern", "Example", "São Paulo", WorkMode.Hybrid,
            "R$ 1.800", "CIEE", "https://example.com/job/1", "Initial description", "QA", DateTimeOffset.UtcNow);
        await repository.UpsertManyAsync([initial], 100, CancellationToken.None);
        var saved = Assert.Single(await repository.GetAllAsync(CancellationToken.None));
        var tracked = await repository.GetByIdAsync(saved.Id, CancellationToken.None);
        Assert.NotNull(tracked);
        tracked!.SetFavorite(true);
        tracked.RecordAction("opened");
        await repository.SaveChangesAsync(CancellationToken.None);

        var updated = JobListing.Create("external-1", "QA Engineer Intern", "Example", "Remote", WorkMode.Remote,
            "R$ 2.000", "CIEE", "https://example.com/job/1", "Updated description", "QA", DateTimeOffset.UtcNow);
        await repository.UpsertManyAsync([updated], 100, CancellationToken.None);

        var result = Assert.Single(await repository.GetAllAsync(CancellationToken.None));
        var withHistory = await repository.GetByIdAsync(result.Id, CancellationToken.None);
        Assert.Equal(saved.Id, result.Id);
        Assert.Equal("QA Engineer Intern", result.Title);
        Assert.True(result.IsFavorite);
        Assert.Single(withHistory!.Actions);
        Assert.Equal("opened", withHistory.Actions.Single().Action);
    }
}
