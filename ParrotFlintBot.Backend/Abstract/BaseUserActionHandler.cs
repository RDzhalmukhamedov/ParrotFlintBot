using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Domain;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Abstract;

internal abstract class BaseUserActionHandler : IUserActionHandler
{
    protected readonly IServiceProvider _serviceProvider;
    protected readonly ILogger<IUserActionHandler> _logger;
    protected readonly RabbitMQPublisher _publisher;
    protected readonly RabbitMQConfiguration _rabbitConfig;

    public abstract UserActionType ActionType { get; }

    protected BaseUserActionHandler(IServiceProvider serviceProvider, ILogger<IUserActionHandler> logger,
                                    RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _publisher = publisher;
        _rabbitConfig = rabbitConfig.Value;
    }

    public abstract Task<bool> Process(UserActionInfo info, CancellationToken stoppingToken);

    protected static ProjectInfo ToProjectInfo(Project project)
    {
        return new ProjectInfo()
        {
            ProjectId = project.Id,
            Status = project.Status,
            Link = project.LastUpdateId is null ? project.GetUrlToFullCrawl() : project.GetUrlForUpdate(),
            ProjectName = project.Name,
            PrevStatus = project.Status,
            UpdatesCount = project.UpdatesCount,
            PrevUpdatesCount = project.PrevUpdatesCount,
            LastUpdateId = project.LastUpdateId,
            LastUpdateTitle = project.LastUpdateTitle,
            NeedFullCrawl = project.NeedFullCrawl,
            ProjectIdOnSite = project.ProjectId,
        };
    }
}
