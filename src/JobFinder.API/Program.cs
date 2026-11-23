using System.Text.Json.Serialization;
using JobFinder.Application.Contracts;
using JobFinder.Application.Services;
using JobFinder.Domain.Entities;
using JobFinder.API;
using JobFinder.Infrastructure;
using JobFinder.Persistence;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Serilog;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Host.UseSerilog((context, services, logger) => logger
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddMemoryCache();
builder.Services.AddOutputCache(options =>
    options.AddPolicy("short", policy => policy.Expire(TimeSpan.FromSeconds(20))));
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins(builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ??
                       ["http://localhost:3000", "http://localhost:8080", "http://127.0.0.1:8080"])
        .AllowAnyHeader().AllowAnyMethod()));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("api", limiter =>
    {
        limiter.PermitLimit = 120;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});
builder.Services.AddJobFinderApplication();
builder.Services.AddJobFinderPersistence(builder.Configuration);
if (builder.Configuration.GetValue("JobFinder:EnableBackgroundCollection", true))
    builder.Services.AddHostedService<JobCollectionWorker>();

var app = builder.Build();
app.UseSerilogRequestLogging();
app.UseCors();
app.UseRateLimiter();
app.UseOutputCache();

app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "Job Finder API" }));

var api = app.MapGroup("/api").RequireRateLimiting("api");
api.MapGet("/dashboard", async (JobFinderService service, CancellationToken ct) =>
    Results.Ok(await service.DashboardAsync(ct))).CacheOutput("short");
api.MapGet("/jobs", async (
    string? q, string? category, WorkMode? workMode, string? company, string? location,
    int? minimumScore, bool? requiresSpanish, bool? inclusiveEmployer, bool? favoritesOnly,
    bool? includeHidden, int? page, int? pageSize, ApplicationStatus? status, JobFinderService service, CancellationToken ct) =>
{
    var filter = new JobFilter(q, category, workMode, company, location, minimumScore,
        requiresSpanish, inclusiveEmployer, favoritesOnly ?? false, includeHidden ?? false,
        page ?? 1, pageSize ?? 50, status);
    return Results.Ok(await service.ListAsync(filter, ct));
});
api.MapGet("/favorites", async (JobFinderService service, CancellationToken ct) =>
    Results.Ok(await service.ListAsync(new JobFilter(FavoritesOnly: true), ct)));
api.MapGet("/news", async (JobFinderService service, CancellationToken ct) =>
    Results.Ok(await service.NewsAsync(ct)));
api.MapPost("/news/visit", async (JobFinderService service, CancellationToken ct) =>
    Results.Ok(new { visitedAt = await service.RecordNewsVisitAsync(ct) }));
api.MapPut("/news/jobs/{id:guid}/viewed", async (Guid id, JobFinderService service, CancellationToken ct) =>
    await service.MarkNewsViewedAsync(id, ct) ? Results.NoContent() : Results.NotFound());
api.MapGet("/jobs/{id:guid}", async (Guid id, JobFinderService service, CancellationToken ct) =>
{
    var job = await service.GetAsync(id, ct);
    return job is null ? Results.NotFound() : Results.Ok(job);
});
api.MapGet("/jobs/{id:guid}/actions", async (Guid id, JobFinderService service, CancellationToken ct) =>
{
    var actions = await service.GetActionsAsync(id, ct);
    return actions is null ? Results.NotFound() : Results.Ok(actions);
});
api.MapPut("/jobs/{id:guid}/favorite", async (Guid id, FavoriteRequest request, JobFinderService service, CancellationToken ct) =>
    await service.SetFavoriteAsync(id, request.Favorite, ct) ? Results.NoContent() : Results.NotFound());
api.MapPut("/jobs/{id:guid}/status", async (Guid id, StatusRequest request, JobFinderService service, CancellationToken ct) =>
    await service.SetStatusAsync(id, request.Status, ct) ? Results.NoContent() : Results.NotFound());
api.MapPut("/jobs/{id:guid}/hidden", async (Guid id, HiddenRequest request, JobFinderService service, CancellationToken ct) =>
    await service.SetHiddenAsync(id, request.Hidden, ct) ? Results.NoContent() : Results.NotFound());
api.MapPost("/jobs/{id:guid}/open", async (Guid id, JobFinderService service, CancellationToken ct) =>
{
    var url = await service.RecordOpenAsync(id, ct);
    return url is null ? Results.NotFound() : Results.Ok(new { url });
});
api.MapGet("/sources", (JobFinderService service) => Results.Ok(service.Sources()));
api.MapPut("/sources/{name}/enabled", (string name, SourceEnabledRequest request, JobFinderService service) =>
{
    service.SetSourceEnabled(name, request.Enabled);
    return service.Sources().Any(source => source.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
        ? Results.NoContent()
        : Results.NotFound();
});
api.MapGet("/settings", (JobFinderService service) => Results.Ok(service.Settings()));
api.MapPut("/settings", (SettingsUpdate update, JobFinderService service) =>
{
    service.UpdateSettings(update);
    return Results.Ok(service.Settings());
});
api.MapGet("/statistics", async (JobFinderService service, CancellationToken ct) =>
    Results.Ok(await service.StatisticsAsync(ct))).CacheOutput("short");
api.MapPost("/collect", async (JobFinderService service, CancellationToken ct) =>
    Results.Ok(new { processed = await service.CollectAsync(ct) }));

using (var scope = app.Services.CreateScope())
{
    var database = scope.ServiceProvider.GetRequiredService<JobFinderDbContext>();
    await database.Database.MigrateAsync();
}

app.Run();

public sealed record FavoriteRequest(bool Favorite);
public sealed record StatusRequest(ApplicationStatus Status);
public sealed record HiddenRequest(bool Hidden);
public sealed record SourceEnabledRequest(bool Enabled);

public partial class Program { }
