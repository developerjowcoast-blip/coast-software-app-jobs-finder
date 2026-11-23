using JobFinder.Application.Contracts;

namespace JobFinder.Application.Abstractions;

public interface IJobSourceProvider
{
    string Name { get; }
    string Description { get; }
    bool IsEnabled { get; }
    Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken);
}

public interface IJobFinderSettings
{
    AppSettingsDto Current { get; }
    IReadOnlyList<SourceDto> Sources { get; }
    void Update(SettingsUpdate settings);
    void SetSourceEnabled(string sourceName, bool enabled);
    IReadOnlyList<string> EnabledSourceNames { get; }
}
