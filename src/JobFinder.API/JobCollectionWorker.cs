using JobFinder.Application.Abstractions;
using JobFinder.Application.Services;

namespace JobFinder.API;

public sealed class JobCollectionWorker(
    IServiceScopeFactory scopeFactory,
    IJobFinderSettings settings,
    ILogger<JobCollectionWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
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
