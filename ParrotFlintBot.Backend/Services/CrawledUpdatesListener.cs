using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class CrawledUpdatesListener : RabbitMQListener
{
    private readonly IServiceProvider _serviceProvider;

    public CrawledUpdatesListener(IServiceProvider serviceProvider, IOptions<RabbitMQConfiguration> config,
        ILogger<CrawledUpdatesListener> logger) : base(config, logger, RouteKeyNames.NewCrawledUpdates,
        nameof(CrawledUpdatesListener))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<bool> ProcessMessage(string message, CancellationToken stoppingToken)
    {
        try
        {
            var updatesInfo = JsonSerializer.Deserialize<List<ProjectInfo>>(message);
            if (updatesInfo is null)
            {
                return false;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var updateService = scope.ServiceProvider.GetRequiredService<IProjectsManagerService>();
                return await updateService.ProcessNewUpdates(updatesInfo, stoppingToken);
            }
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing message for routeKey: {RouteKey}, failed with exception: {Exception}",
                RouteKey, ex);
            return false;
        }
    }
}