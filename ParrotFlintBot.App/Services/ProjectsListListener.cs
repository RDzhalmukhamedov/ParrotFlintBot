using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.App.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.App.Services;

public class ProjectsListListener : RabbitMQListener
{
    private readonly IServiceProvider _serviceProvider;

    public ProjectsListListener(
        IServiceProvider serviceProvider,
        ILogger<ProjectsListListener> logger,
        IOptions<RabbitMQConfiguration> config)
        : base(config, logger, RouteKeyNames.ProjectsList, nameof(ProjectsListListener))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<bool> ProcessMessage(string message, CancellationToken stoppingToken)
    {
        try
        {
            var updatesInfo = JsonSerializer.Deserialize<UpdatesNotification>(message);
            if (updatesInfo is null)
            {
                return false;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var senderService = scope.ServiceProvider.GetRequiredService<ICommunicationService>();
                await senderService.SendProjectsLists(updatesInfo, stoppingToken);
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing message for routeKey: {RouteKey}, failed with exception: {Exception}",
                RouteKey, ex);
            return false;
        }
    }
}