using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IKSCrawlerUnitOfWork _db;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly RabbitMQPublisher _publisher;
    private readonly RabbitMQConfiguration _rabbitConfig;
    private readonly string _crawlRouteKey;
    private readonly string _updatesRouteKey;

    public SubscriptionService(IKSCrawlerUnitOfWork dbConnection, ILogger<SubscriptionService> logger,
        RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _db = dbConnection;
        _logger = logger;
        
        _publisher = publisher;
        _rabbitConfig = rabbitConfig.Value;
        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsToCrawl, out var crawlRoute);
        _crawlRouteKey = string.IsNullOrWhiteSpace(crawlRoute) ? RouteKeyNames.ProjectsToCrawl : crawlRoute;
        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.UpdatesNotification, out var updatesRoute);
        _updatesRouteKey = string.IsNullOrWhiteSpace(updatesRoute) ? RouteKeyNames.UpdatesNotification : updatesRoute;
    }

    public async Task<bool> ProcessSubscribe(UserActionInfo info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called subscription to project {Project} updates from {ChatId}.",
            info.ProjectLink, info.ChatId);
        var result = false;
        Project? project = null;
        try
        {
            var user = await _db.Users.CreateIfNotExist(info.ChatId, stoppingToken);
            if (info.ProjectLink is not null)
            {
                var currentSubscription = user.Projects.ContainsEqualProject(info.ProjectLink);
                if (currentSubscription is null)
                {
                    project = await _db.Projects.CreateIfNotExist(info.ProjectLink.GetProjectSlug(),
                        info.ProjectLink.GetCreatorSlug(), info.ProjectLink.GetSiteName(), stoppingToken);
                    user.Projects.Add(project);
                }
                else
                {
                    project = currentSubscription;
                }
            }

            await _db.Commit(stoppingToken);
            if (project is not null && project.Status == ProjectStatus.NotTracked)
            {
                var projectInfo = new ProjectInfo()
                {
                    ProjectId = project.Id,
                    Status = project.Status,
                    Link = project.GetUrlToCrawl(),
                    ProjectName = project.Name,
                    PrevStatus = project.Status,
                    UpdatesCount = project.UpdatesCount,
                    PrevUpdatesCount = project.PrevUpdatesCount
                };
                var message = JsonSerializer.Serialize(new[] { projectInfo });
                _publisher.PushMessage(_crawlRouteKey,
                    message,
                    _rabbitConfig.MessageTTL);
            }
            else if (project is not null)
            {
                var projectNotification = new UpdatesNotification()
                {
                    ChatId = user.ChatId,
                    Updates = new[]
                    {
                        new ProjectInfo()
                        {
                            ProjectId = project.Id,
                            Status = project.Status,
                            Link = project.LastUpdateId is null ? project.GetUrlToCrawl() : project.GetUrlForUpdate(),
                            ProjectName = project.Name,
                            PrevStatus = project.Status,
                            UpdatesCount = project.UpdatesCount,
                            PrevUpdatesCount = project.PrevUpdatesCount,
                            LastUpdateId = project.LastUpdateId,
                            LastUpdateTitle = project.LastUpdateTitle
                        }
                    }.ToList()
                };
                _publisher.PushMessage(_updatesRouteKey,
                    new []{ projectNotification },
                    _rabbitConfig.MessageTTL);
            }
            result = true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Subscription to project updates failed with exception {Exception}", ex);
        }

        return result;
    }

    public async Task<bool> ProcessUnsubscribe(UserActionInfo info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called unsubscription to project {Project} updates from {ChatId}.",
            info.ProjectLink, info.ChatId);
        var result = false;
        try
        {
            var user = await _db.Users.GetByChatId(info.ChatId, stoppingToken, includeProjects: true);
            if (info.ProjectLink is not null)
            {
                var project = await _db.Projects.GetByProjectSlug(info.ProjectLink.GetProjectSlug(), stoppingToken);
                if (user is not null && project is not null)
                {
                    user.Projects.Remove(project);
                }
                else
                {
                    return result;
                }
            }

            await _db.Commit(stoppingToken);
            result = true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Unsubscription to project updates failed with exception {Exception}", ex);
        }

        return result;
    }
}