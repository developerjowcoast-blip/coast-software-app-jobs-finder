using JobFinder.Application.Abstractions;
using JobFinder.Application.Services;
using JobFinder.Infrastructure.Settings;
using JobFinder.Infrastructure.Sources;
using Microsoft.Extensions.DependencyInjection;

namespace JobFinder.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddJobFinderApplication(this IServiceCollection services)
    {
        services.AddSingleton<CompatibilityScorer>();
        services.AddScoped<JobFinderService>();
        services.AddSingleton<IJobFinderSettings, JobFinderSettings>();
        services.AddHttpClient<GupyJobSourceProvider>(client =>
        {
            client.BaseAddress = new Uri("https://portal.gupy.io/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobFinderADS/1.0 (+public-job-aggregation)");
        });
        services.AddSingleton<IJobSourceProvider>(provider =>
            provider.GetRequiredService<GupyJobSourceProvider>());
        services.AddHttpClient<NubeJobSourceProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.nube.com.br/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobFinderADS/1.0 (+public-job-aggregation)");
        });
        services.AddSingleton<IJobSourceProvider>(provider =>
            provider.GetRequiredService<NubeJobSourceProvider>());
        services.AddHttpClient<CieeJobSourceProvider>(client =>
        {
            client.BaseAddress = new Uri("https://api-pp.ciee.org.br/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobFinderADS/1.0 (+public-job-aggregation)");
        });
        services.AddSingleton<IJobSourceProvider>(provider =>
            provider.GetRequiredService<CieeJobSourceProvider>());
        services.AddHttpClient<TramposJobSourceProvider>(client =>
        {
            client.BaseAddress = new Uri("https://trampos.co/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobFinderADS/1.0 (+public-job-aggregation)");
        });
        services.AddSingleton<IJobSourceProvider>(provider =>
            provider.GetRequiredService<TramposJobSourceProvider>());
        services.AddHttpClient<VagasComBrJobSourceProvider>(client =>
        {
            client.BaseAddress = new Uri("https://www.vagas.com.br/");
            client.Timeout = TimeSpan.FromSeconds(30);
            client.DefaultRequestHeaders.Accept.ParseAdd("text/html,application/xhtml+xml");
            client.DefaultRequestHeaders.UserAgent.ParseAdd("JobFinderADS/1.0 (+public-job-aggregation)");
        });
        services.AddSingleton<IJobSourceProvider>(provider =>
            provider.GetRequiredService<VagasComBrJobSourceProvider>());
        foreach (var source in new[] { "Remotar" })
            services.AddSingleton<IJobSourceProvider>(_ => new SampleJobSourceProvider(source));
        return services;
    }
}
