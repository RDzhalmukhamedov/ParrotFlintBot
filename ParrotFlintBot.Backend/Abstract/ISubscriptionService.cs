using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Abstract;

public interface ISubscriptionService
{
    Task<bool> ProcessSubscribe(UserActionInfo info, CancellationToken stoppingToken);
    
    Task<bool> ProcessUnsubscribe(UserActionInfo info, CancellationToken stoppingToken);
}