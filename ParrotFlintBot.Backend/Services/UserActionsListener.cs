using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Services.UserActions;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;
using System.Text.Json;

namespace ParrotFlintBot.Backend.Services;

internal class UserActionsListener : RabbitMQListener
{
    private readonly UserActionHandlerFactory _actionFactory;

    public UserActionsListener(UserActionHandlerFactory actionFactory, IOptions<RabbitMQConfiguration> config,
        ILogger<UserActionsListener> logger) : base(config, logger, RouteKeyNames.UserActions,
        nameof(UserActionsListener))
    {
        _actionFactory = actionFactory;
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

            return await _actionFactory.GetActionHandler(actionInfo.Type).Process(actionInfo, stoppingToken);
        }
        catch (Exception ex)
        {
            Logger.LogError("Processing message for routeKey: {RouteKey}, failed with exception: {Exception}",
                RouteKey, ex);
            return false;
        }
    }
}