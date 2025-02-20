using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;
using System.Text.Json;

namespace ParrotFlintBot.Backend.Services.UserActions;

internal class SubscribeAction : BaseUserActionHandler
{
    private readonly string _fullCrawlRouteKey;
    private readonly string _updatesRouteKey;

    public override UserActionType ActionType => UserActionType.Subscribe;

    public SubscribeAction(IServiceProvider serviceProvider, ILogger<SubscribeAction> logger,
        RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
        : base(serviceProvider, logger, publisher, rabbitConfig)
    {
        _rabbitConfig.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsToFullCrawl, out var fullCrawlRoute);
        _fullCrawlRouteKey = string.IsNullOrWhiteSpace(fullCrawlRoute) ? RouteKeyNames.ProjectsToFullCrawl : fullCrawlRoute;
        _rabbitConfig.PublisherRouteKeys.TryGetValue(RouteKeyNames.UpdatesNotification, out var updatesRoute);
        _updatesRouteKey = string.IsNullOrWhiteSpace(updatesRoute) ? RouteKeyNames.UpdatesNotification : updatesRoute;
    }

    public async override Task<bool> Process(UserActionInfo info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called subscription to project {Project} updates from {UserId}({ChatId}).",info.ProjectLink, info.UserId, info.ChatId);
        try
        {
            var user = await CheckAndGetUser(info, stoppingToken);
            var project = await CheckAndGetProject(info, user, stoppingToken);

            // If project is new for us, trying to get info about it
            if (project.Status == ProjectStatus.NotTracked)
            {
                var projectInfo = ToProjectInfo(project);
                var message = JsonSerializer.Serialize(new[] { projectInfo });
                _publisher.PushMessage(_fullCrawlRouteKey, message, _rabbitConfig.MessageTTL);

                return true;
            }

            // If we know something about project, send it info to user
            var projectNotification = new UpdatesNotification()
            {
                ChatId = user.ChatId,
                Updates = new List<ProjectInfo>() { ToProjectInfo(project) }
            };
            _publisher.PushMessage(_updatesRouteKey, new[] { projectNotification }, _rabbitConfig.MessageTTL);
        }
        catch (Exception ex)
        {
            _logger.LogError("Subscription to project updates failed with exception {Exception}", ex);
            return false;
        }

        return true;
    }

    private async Task<User> CheckAndGetUser(UserActionInfo info, CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
            User? user = null;
            if (info.ChatId is null)
            {
                if (info.UserId is not null)
                {
                    user = await db.Users.GetByUserId(info.UserId, stoppingToken, includeProjects: true);
                }

                if (user is null) throw new NullReferenceException($"{nameof(info.UserId)} and {nameof(info.ChatId)}");
            }

            user = user ?? await db.Users.CreateIfNotExist(info.ChatId!.Value, stoppingToken);
            if (!string.IsNullOrEmpty(info.UserId))
            {
                user.UserId = info.UserId;
                db.Users.Update(user);
            }
            await db.Commit(stoppingToken);

            return user;
        }
    }

    private async Task<Project> CheckAndGetProject(UserActionInfo info, User user, CancellationToken stoppingToken)
    {
        if (info.ProjectLink is null) throw new NullReferenceException(nameof(info.ProjectLink));

        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
            Project project;
            var currentSubscription = user.Projects.ContainsEqualProject(info.ProjectLink);
            if (currentSubscription is null)
            {
                project = await db.Projects.CreateIfNotExist(info.ProjectLink.GetProjectSlug(),
                    info.ProjectLink.GetCreatorSlug(), info.ProjectLink.GetSiteName(), stoppingToken);
                user.Projects.Add(project);
            }
            else
            {
                project = currentSubscription;
            }
            await db.Commit(stoppingToken);

            return project;
        }
    }
}
