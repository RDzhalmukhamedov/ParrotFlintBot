using Microsoft.Extensions.Logging;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IKSCrawlerUnitOfWork _db;
    private readonly ILogger<SubscriptionService> _logger;

    public SubscriptionService(IKSCrawlerUnitOfWork dbConnection, ILogger<SubscriptionService> logger)
    {
        _db = dbConnection;
        _logger = logger;
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
                    info.ProjectLink.GetCreatorSlug(), stoppingToken);
                user.Projects.Add(project);
            }

            await _db.Commit(stoppingToken);
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