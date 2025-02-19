using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Abstract;

internal abstract class BaseUserActionHandler : IUserActionHandler, IDisposable
{
    protected readonly IKSCrawlerUnitOfWork _db;
    protected readonly ILogger<IUserActionHandler> _logger;
    protected readonly RabbitMQPublisher _publisher;
    protected readonly RabbitMQConfiguration _rabbitConfig;
    private bool disposedValue;

    public abstract UserActionType ActionType { get; }

    protected BaseUserActionHandler(IKSCrawlerUnitOfWork dbConnection, ILogger<IUserActionHandler> logger,
                                    RabbitMQPublisher publisher, IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _db = dbConnection;
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

    protected virtual void Dispose(bool disposing)
    {
        if (!disposedValue)
        {
            if (disposing)
            {
                _db.Dispose();
            }

            disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
