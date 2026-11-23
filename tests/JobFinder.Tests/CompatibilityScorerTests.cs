using JobFinder.Application.Contracts;
using JobFinder.Application.Services;
using JobFinder.Domain.Entities;

namespace JobFinder.Tests;

public sealed class CompatibilityScorerTests
{
    private readonly CompatibilityScorer _scorer = new();

    [Fact]
    public void ScoresRelevantRemoteSpanishInclusiveInternshipAtMaximum()
    {
        var job = new JobListingInput("1", "QA Intern - Spanish", "Example", "Brasil", WorkMode.Remote, null,
            "CIEE", "https://example.com/job/1",
            "Estágio para estudante de ADS em Quality Assurance. Espanhol e apoio ao time LATAM; programa inclusivo.",
            "QA", DateTimeOffset.UtcNow);

        var result = _scorer.Score(job);

        Assert.Equal(100, result.Score);
        Assert.True(result.RequiresSpanish);
        Assert.True(result.Inclusive);
        Assert.Contains(result.Reasons, reason => reason.Contains("espanhol", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DoesNotUseCandidateDisabilityAsAnExclusionCriterion()
    {
        var job = new JobListingInput("2", "Software Engineer", "Example", "São Paulo", WorkMode.OnSite, null,
            "Gupy", "https://example.com/job/2", "Vaga para desenvolvimento de software.", "Desenvolvimento", DateTimeOffset.UtcNow);

        var result = _scorer.Score(job);

        Assert.InRange(result.Score, 0, 100);
        Assert.DoesNotContain(result.Reasons, reason => reason.Contains("TEA", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void JobListingRejectsNonHttpUrl()
    {
        Assert.Throws<ArgumentException>(() => JobListing.Create("1", "QA Intern", "Example", "Brasil", WorkMode.Remote,
            null, "CIEE", "file:///private.txt", "Job", "QA", DateTimeOffset.UtcNow));
    }
}
