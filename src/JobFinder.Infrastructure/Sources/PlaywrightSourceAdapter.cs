using JobFinder.Application.Abstractions;
using JobFinder.Application.Contracts;

namespace JobFinder.Infrastructure.Sources;

public interface IPublicJobPageReader
{
    Task<IReadOnlyCollection<JobListingInput>> ReadPublicListingsAsync(string sourceName, CancellationToken cancellationToken);
}

public abstract class PlaywrightSourceAdapter(
    IPublicJobPageReader pageReader,
    string sourceName,
    string description) : IJobSourceProvider
{
    public string Name { get; } = sourceName;
    public string Description { get; } = description;
    public bool IsEnabled => true;

    public Task<IReadOnlyCollection<JobListingInput>> FetchAsync(CancellationToken cancellationToken) =>
        pageReader.ReadPublicListingsAsync(Name, cancellationToken);
}
