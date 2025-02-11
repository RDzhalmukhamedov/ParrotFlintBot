using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services.UserActions;

internal class UserActionHandlerFactory
{
    private readonly IEnumerable<IUserActionHandler> _actionHandlers;

    public UserActionHandlerFactory(IEnumerable<IUserActionHandler> actionHandlers)
    {
        _actionHandlers = actionHandlers;
    }

    public IUserActionHandler GetActionHandler(UserActionType actionType)
    {
        return _actionHandlers.FirstOrDefault(e => e.ActionType == actionType)
            ?? throw new NotSupportedException();
    }
}
