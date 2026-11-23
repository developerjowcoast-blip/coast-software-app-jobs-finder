using JobFinder.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace JobFinder.Persistence;

public sealed class JobFinderDbContext(DbContextOptions<JobFinderDbContext> options) : DbContext(options)
{
    public DbSet<JobListing> Jobs => Set<JobListing>();
    public DbSet<CandidateAction> CandidateActions => Set<CandidateAction>();
    public DbSet<UserVisitState> UserVisitStates => Set<UserVisitState>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var job = modelBuilder.Entity<JobListing>();
        job.ToTable("JobListings");
        job.HasKey(entity => entity.Id);
        job.HasIndex(entity => new { entity.Source, entity.ExternalId }).IsUnique();
        job.HasIndex(entity => entity.PublishedAt);
        job.HasIndex(entity => entity.CompatibilityScore);
        job.HasIndex(entity => new { entity.IsFavorite, entity.IsHidden });
        job.HasIndex(entity => entity.FirstSeenAt);
        job.HasIndex(entity => entity.ScoreChangedAt);
        job.Property(entity => entity.Title).HasMaxLength(300).IsRequired();
        job.Property(entity => entity.Company).HasMaxLength(200).IsRequired();
        job.Property(entity => entity.Location).HasMaxLength(300);
        job.Property(entity => entity.Source).HasMaxLength(100).IsRequired();
        job.Property(entity => entity.ExternalId).HasMaxLength(300).IsRequired();
        job.Property(entity => entity.Url).HasMaxLength(2048).IsRequired();
        job.Property(entity => entity.Summary).HasMaxLength(1000);
        job.Property(entity => entity.Category).HasMaxLength(100);
        job.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(30);
        job.Property(entity => entity.WorkMode).HasConversion<string>().HasMaxLength(30);
        job.HasMany(entity => entity.Actions).WithOne().HasForeignKey(action => action.JobListingId)
            .OnDelete(DeleteBehavior.Cascade);

        var action = modelBuilder.Entity<CandidateAction>();
        action.ToTable("CandidateActions");
        action.HasKey(entity => entity.Id);
        action.HasIndex(entity => entity.OccurredAt);
        action.Property(entity => entity.Action).HasMaxLength(100).IsRequired();

        var visit = modelBuilder.Entity<UserVisitState>();
        visit.ToTable("UserVisitStates");
        visit.HasKey(entity => entity.Id);
    }

}

public static class DependencyInjection
{
    public static IServiceCollection AddJobFinderPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("JobFinder") ?? "Data Source=jobfinder.db";
        services.AddDbContext<JobFinderDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<JobFinder.Application.Abstractions.IJobRepository, JobRepository>();
        return services;
    }
}
