using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services.UserActions;

internal class UnsubscribeAction : BaseUserActionHandler
{
    public override UserActionType ActionType => UserActionType.Unsubscribe;

    public UnsubscribeAction(IServiceProvider serviceProvider, ILogger<UnsubscribeAction> logger,
        RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
        : base(serviceProvider, logger, publisher, rabbitConfig)
    {
    }

    public async override Task<bool> Process(UserActionInfo info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called unsubscription to project {Project} updates from {UserId}({ChatId}).", info.ProjectLink, info.UserId, info.ChatId);
        var result = false;
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();

                var user = info.ChatId is not null
                    ? await db.Users.GetByChatId(info.ChatId.Value, stoppingToken, includeProjects: true)
                    : info.UserId is not null
                        ? await db.Users.GetByUserId(info.UserId, stoppingToken, includeProjects: true)
                        : null;

                if (user is null) throw new NullReferenceException(nameof(user));

                if (info.ProjectLink is null) throw new NullReferenceException(nameof(info.ProjectLink));

                var project = await db.Projects.GetByProjectSlug(info.ProjectLink.GetProjectSlug(), stoppingToken);
                if (project is not null)
                {
                    user.Projects.Remove(project);
                }
                else
                {
                    return result;
                }

                await db.Commit(stoppingToken);
                result = true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Unsubscription to project updates failed with exception {Exception}", ex);
        }

        return result;
    }

    //private async Task<User> GetUser(UserActionInfo info, CancellationToken stoppingToken)
    //{
    //    var user = info.ChatId is not null
    //        ? await _db.Users.GetByChatId(info.ChatId.Value, stoppingToken, includeProjects: true)
    //        : info.UserId is not null
    //            ? await _db.Users.GetByUserId(info.UserId, stoppingToken, includeProjects: true)
    //            : null;

    //    if (user is null) throw new NullReferenceException(nameof(user));

    //    return user;
    //}
}
