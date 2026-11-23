using JobFinder.Application.Abstractions;
using JobFinder.Application.Services;
using JobFinder.Persistence;
using Microsoft.EntityFrameworkCore;

namespace JobFinder.Worker;

public sealed class Worker(
    IServiceScopeFactory scopeFactory,
    IJobFinderSettings settings,
    ILogger<Worker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using (var scope = scopeFactory.CreateScope())
        {
            var database = scope.ServiceProvider.GetRequiredService<JobFinderDbContext>();
            await database.Database.EnsureCreatedAsync(stoppingToken);
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            for (var attempt = 1; attempt <= 3; attempt++)
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var service = scope.ServiceProvider.GetRequiredService<JobFinderService>();
                    var processed = await service.CollectAsync(stoppingToken);
                    logger.LogInformation("Collection completed; {Count} listings processed.", processed);
                    break;
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { return; }
                catch (Exception exception)
                {
                    logger.LogWarning(exception, "Collection attempt {Attempt} failed.", attempt);
                    if (attempt < 3)
                        await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, attempt)), stoppingToken);
                }
            }

            await Task.Delay(TimeSpan.FromMinutes(settings.Current.RefreshIntervalMinutes), stoppingToken);
        }
    }
}
