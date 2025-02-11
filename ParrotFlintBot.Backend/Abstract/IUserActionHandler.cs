using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Abstract;

internal interface IUserActionHandler
{
    UserActionType ActionType { get; }

    Task<bool> Process(UserActionInfo info, CancellationToken stoppingToken);
}
