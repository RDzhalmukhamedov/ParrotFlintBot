using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.App.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.App.Services;

public class UpdatesNotificationListener : RabbitMQListener
{
    private readonly IServiceProvider _serviceProvider;

    public UpdatesNotificationListener(
        IServiceProvider serviceProvider,
        ILogger<UpdatesNotificationListener> logger,
        IOptions<RabbitMQConfiguration> config)
        : base(config, logger, RouteKeyNames.UpdatesNotification, nameof(UpdatesNotificationListener))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<bool> ProcessMessage(string message, CancellationToken stoppingToken)
    {
        try
        {
            var updatesInfo = JsonSerializer.Deserialize<List<UpdatesNotification>>(message);
            if (updatesInfo.IsNullOrEmpty())
            {
                return false;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var senderService = scope.ServiceProvider.GetRequiredService<ICommunicationService>();
                await senderService.SendProjectUpdates(updatesInfo!, stoppingToken);
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