using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Services.UserActions;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;
using System.Text.Json;

namespace ParrotFlintBot.Backend.Services;

internal class UserActionsListener : RabbitMQListener
{
    private readonly IServiceProvider _serviceProvider;

    public UserActionsListener(IServiceProvider serviceProvider, IOptions<RabbitMQConfiguration> config,
        ILogger<UserActionsListener> logger) : base(config, logger, RouteKeyNames.UserActions,
        nameof(UserActionsListener))
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task<bool> ProcessMessage(string message, CancellationToken stoppingToken)
    {
        try
        {
            var actionInfo = JsonSerializer.Deserialize<UserActionInfo>(message);
            if (actionInfo is null)
            {
                return false;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var actionFactory = scope.ServiceProvider.GetRequiredService<UserActionHandlerFactory>();
                return await actionFactory.GetActionHandler(actionInfo.Type).Process(actionInfo, stoppingToken);
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