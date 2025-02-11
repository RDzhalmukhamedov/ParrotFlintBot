using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services.UserActions;

internal class ListOfSubsAction : BaseUserActionHandler
{
    private readonly string _projectsListRouteKey;

    public override UserActionType ActionType => UserActionType.Unsubscribe;

    public ListOfSubsAction(IKSCrawlerUnitOfWork dbConnection, ILogger<ListOfSubsAction> logger,
        RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
        : base(dbConnection, logger, publisher, rabbitConfig)
    {
        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsList, out var route);
        _projectsListRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.ProjectsList : route;
    }

    public async override Task<bool> Process(UserActionInfo info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started getting list of projects for user {UserId}({ChatId}).", info.UserId, info.ChatId);
        try
        {
            var user = await GetUser(info, stoppingToken);
            if (user is not null)
            {
                var projectInfos = user.Projects.Select(ToProjectInfo);
                var notification = new UpdatesNotification()
                {
                    ChatId = user.ChatId,
                    UserId = info.UserId,
                    Updates = projectInfos.ToList()
                };
                _publisher.PushMessage(_projectsListRouteKey, notification, _rabbitConfig.MessageTTL);

                return true;
            }
            else
            {
                if (info.ChatId is null) return false;

                var notification = new UpdatesNotification()
                {
                    ChatId = info.ChatId.Value,
                    UserId = info.UserId,
                    Updates = new List<ProjectInfo>()
                };
                _publisher.PushMessage(_projectsListRouteKey, notification, _rabbitConfig.MessageTTL);

                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Updating information about projects failed with exception {Exception}", ex);
        }

        return false;
    }

    private async Task<User?> GetUser(UserActionInfo info, CancellationToken stoppingToken)
    {
        var user = info.ChatId is not null
            ? await _db.Users.GetByChatId(info.ChatId.Value, stoppingToken, includeProjects: true)
            : info.UserId is not null
                ? await _db.Users.GetByUserId(info.UserId, stoppingToken, includeProjects: true)
                : null;

        return user;
    }
}
