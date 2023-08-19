using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IKSCrawlerUnitOfWork _db;
    private readonly ILogger<SubscriptionService> _logger;
    private readonly RabbitMQPublisher _publisher;
    private readonly RabbitMQConfiguration _rabbitConfig;
    private readonly string _routeKey;

    public SubscriptionService(IKSCrawlerUnitOfWork dbConnection, ILogger<SubscriptionService> logger,
        RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _db = dbConnection;
        _logger = logger;
        
        _publisher = publisher;
        _rabbitConfig = rabbitConfig.Value;
        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsToCrawl, out var route);
        _routeKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.ProjectsToCrawl : route;
    }

    public async Task<bool> ProcessSubscribe(UserActionInfo info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called subscription to project updates.");
        var result = false;
        try
        {
            var user = await _db.Users.CreateIfNotExist(info.ChatId, stoppingToken);
            if (info.ProjectLink is not null)
            {
                var project = await _db.Projects.CreateIfNotExist(info.ProjectLink.GetProjectSlug(),
                    info.ProjectLink.GetCreatorSlug(), info.ProjectLink.GetSiteName(), stoppingToken);
                user.Projects.Add(project);
            }

            await _db.Commit(stoppingToken);
            if (user.Projects.Any(p => p.Status == ProjectStatus.NotTracked))
            {
                _publisher.PushMessage(_routeKey,
                    JsonSerializer.Serialize(user.Projects.Where(p => p.Status == ProjectStatus.NotTracked)),
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
        _logger.LogInformation("Called unsubscription to project updates.");
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