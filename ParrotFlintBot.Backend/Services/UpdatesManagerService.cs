using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class UpdatesManagerService : IUpdatesManagerService
{
    private readonly ILogger<UpdatesManagerService> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly RabbitMQPublisher _publisher;
    private readonly RabbitMQConfiguration _rabbitConfig;
    private readonly string _updatesRouteKey;

    public UpdatesManagerService(
        IServiceProvider serviceProvider,
        RabbitMQPublisher publisher,
        ILogger<UpdatesManagerService> logger,
        IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _serviceProvider = serviceProvider;
        _publisher = publisher;
        _logger = logger;
        _rabbitConfig = rabbitConfig.Value;

        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.UpdatesNotification, out var route);
        _updatesRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.UpdatesNotification : route;
    }

    public async Task<bool> ProcessNewUpdates(List<ProjectInfo> updatesInfo, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Started updating information about {Count} projects.", updatesInfo.Count);
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
                await db.Projects.BulkUpdate(updatesInfo, stoppingToken);
                await db.Commit(stoppingToken);
                var users = await db.Users.GetUsersWithSubscriptions(stoppingToken);
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
                        Updates = updates.Where(u => !u.NeedFullCrawl).ToList()
                    };
                }).Where(n => !n.Updates.IsNullOrEmpty());

                _publisher.PushMessage(_updatesRouteKey, updateNotifications, _rabbitConfig.MessageTTL);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError("Updating information about projects failed with exception {Exception}", ex);
            return false;
        }
    }
}