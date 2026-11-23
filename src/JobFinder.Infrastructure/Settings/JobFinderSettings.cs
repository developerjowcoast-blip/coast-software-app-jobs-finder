using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;
using Microsoft.Extensions.Configuration;

namespace JobFinder.Infrastructure.Settings;

public sealed class JobFinderSettings : IJobFinderSettings
{
    private readonly object _lock = new();
    private readonly Dictionary<string, SourceDto> _sources;
    private int _refreshIntervalMinutes = 30;
    private int _maximumRecords = 5000;
    private string _theme = "system";

    public JobFinderSettings(IEnumerable<IJobSourceProvider> providers, IConfiguration configuration)
    {
        _refreshIntervalMinutes = Math.Clamp(
            int.TryParse(configuration["JobFinder:RefreshIntervalMinutes"], out var interval) ? interval : 30, 5, 1440);
        _maximumRecords = Math.Clamp(
            int.TryParse(configuration["JobFinder:MaximumRecords"], out var maximum) ? maximum : 5000, 100, 100000);
        _theme = configuration["JobFinder:Theme"] ?? "system";
        _sources = providers.ToDictionary(
            provider => provider.Name,
            provider => new SourceDto(provider.Name, provider.IsEnabled, provider.Description, null),
            StringComparer.OrdinalIgnoreCase);
    }

    public AppSettingsDto Current
    {
        get { lock (_lock) return new AppSettingsDto(_refreshIntervalMinutes, _maximumRecords, _theme, "SQLite"); }
    }

    public IReadOnlyList<SourceDto> Sources
    {
        get { lock (_lock) return _sources.Values.OrderBy(source => source.Name).ToArray(); }
    }

    public IReadOnlyList<string> EnabledSourceNames
    {
        get { lock (_lock) return _sources.Values.Where(source => source.Enabled).Select(source => source.Name).ToArray(); }
    }

    public void Update(SettingsUpdate settings)
    {
        lock (_lock)
        {
            if (settings.RefreshIntervalMinutes.HasValue)
                _refreshIntervalMinutes = Math.Clamp(settings.RefreshIntervalMinutes.Value, 5, 1440);
            if (settings.MaximumRecords.HasValue)
                _maximumRecords = Math.Clamp(settings.MaximumRecords.Value, 100, 100000);
            if (!string.IsNullOrWhiteSpace(settings.Theme))
            {
                var theme = settings.Theme.Trim().ToLowerInvariant();
                if (theme is "system" or "light" or "dark") _theme = theme;
            }
        }
    }

    public void SetSourceEnabled(string sourceName, bool enabled)
    {
        lock (_lock)
        {
            if (_sources.TryGetValue(sourceName, out var source))
                _sources[sourceName] = source with { Enabled = enabled };
        }
    }
}
