using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans.Hosting;
using SampleOrleans.Host;
using SampleOrleans.Infrastructure;

var builder = Host.CreateApplicationBuilder(args);

builder.Logging.SetMinimumLevel(LogLevel.Warning);
builder.Logging.AddFilter("SampleOrleans", LogLevel.Information);

builder.UseOrleans(silo =>
{
    silo.UseLocalhostClustering();
    silo.AddOrderGrains();
});

using var host = builder.Build();

await host.StartAsync();

var grains = host.Services.GetRequiredService<IGrainFactory>();
var passed = await Demo.RunAsync(grains);

await host.StopAsync();

return passed ? 0 : 1;
