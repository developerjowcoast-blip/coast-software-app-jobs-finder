using JobFinder.Infrastructure;
using JobFinder.Persistence;
using JobFinder.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddSerilog((services, logger) => logger
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Console());
builder.Services.AddJobFinderApplication();
builder.Services.AddJobFinderPersistence(builder.Configuration);
builder.Services.AddHostedService<Worker>();

await builder.Build().RunAsync();
