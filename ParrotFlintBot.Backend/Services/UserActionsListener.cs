using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class UserActionsListener : RabbitMQListener
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
            var subscriptionInfo = JsonSerializer.Deserialize<UserActionInfo>(message);
            if (subscriptionInfo is null)
            {
                return false;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                Task<bool>? result;
                ISubscriptionService? subscriptionService;
                switch (subscriptionInfo.Type)
                {
                    case UserActionType.Subscribe:
                        subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                        result = subscriptionService.ProcessSubscribe(subscriptionInfo, stoppingToken);
                        break;
                    case UserActionType.Unsubscribe:
                        subscriptionService = scope.ServiceProvider.GetRequiredService<ISubscriptionService>();
                        result = subscriptionService.ProcessUnsubscribe(subscriptionInfo, stoppingToken);
                        break;
                    case UserActionType.List:
                        var updatesService = scope.ServiceProvider.GetRequiredService<IProjectsManagerService>();
                        result = updatesService.ProcessProjectsList(subscriptionInfo.ChatId, stoppingToken);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(subscriptionInfo.Type));
                }

                return await result;
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