using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class ProjectsManagerService : IProjectsManagerService
{
    private readonly ILogger<ProjectsManagerService> _logger;
    private readonly IKSCrawlerUnitOfWork _db;
    private readonly RabbitMQPublisher _publisher;
    private readonly RabbitMQConfiguration _rabbitConfig;
    private readonly string _updatesRouteKey;
    private readonly string _projectsListRouteKey;

    public ProjectsManagerService(
        IKSCrawlerUnitOfWork dbConnection,
        RabbitMQPublisher publisher,
        ILogger<ProjectsManagerService> logger,
        IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _db = dbConnection;
        _publisher = publisher;
        _logger = logger;
        _rabbitConfig = rabbitConfig.Value;

        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.UpdatesNotification, out var route);
        _updatesRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.UpdatesNotification : route;
        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsList, out route);
        _projectsListRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.ProjectsList : route;
    }

    public async Task<bool> ProcessNewUpdates(List<ProjectInfo> updatesInfo, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started updating information about projects.");
        try
        {
            await _db.Projects.BulkUpdate(updatesInfo, stoppingToken);
            await _db.Commit(stoppingToken);
            var users = await _db.Users.GetUsersWithSubscriptions(stoppingToken);
            var updateNotifications = users.Select((user) =>
            {
                var updates = updatesInfo.Join(
                    user.Projects,
                    update => update.ProjectId,
                    project => project.Id,
                    (update, _) => update);
                return new UpdatesNotification()
                {
                    ChatId = user.ChatId,
                    Updates = updates.ToList()
                };
            });
            
            _publisher.PushMessage(_updatesRouteKey, updateNotifications, _rabbitConfig.MessageTTL);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Updating information about projects failed with exception {Exception}", ex);
            return false;
        }
    }

    public async Task<bool> ProcessProjectsList(long chatId, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started getting information about projects for user.");
        try
        {
            var user = await _db.Users.GetByChatId(chatId, stoppingToken, true);
            if (user is not null)
            {
                var projectInfos = user.Projects
                    .Select(p => new ProjectInfo()
                    {
                        ProjectId = p.Id,
                        Status = p.Status,
                        Link = p.LastUpdateId is null ? p.GetUrlToCrawl() : p.GetUrlForUpdate(),
                        ProjectName = p.Name,
                        PrevStatus = p.Status,
                        UpdatesCount = p.UpdatesCount,
                        PrevUpdatesCount = p.PrevUpdatesCount
                    });
                var result = new UpdatesNotification()
                {
                    ChatId = user.ChatId,
                    Updates = projectInfos.ToList()
                };
                _publisher.PushMessage(_projectsListRouteKey, result, _rabbitConfig.MessageTTL);
                return true;
            }
            else
            {
                var result = new UpdatesNotification()
                {
                    ChatId = chatId,
                    Updates = new List<ProjectInfo>()
                };
                _publisher.PushMessage(_projectsListRouteKey, result, _rabbitConfig.MessageTTL);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogInformation("Updating information about projects failed with exception {Exception}", ex);
        }
        return false;
    }
}