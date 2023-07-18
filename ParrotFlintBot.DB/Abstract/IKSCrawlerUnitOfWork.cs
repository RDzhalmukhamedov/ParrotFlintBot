namespace ParrotFlintBot.DB.Abstract;

public interface IKSCrawlerUnitOfWork : IDisposable
{
    IUserRepository Users { get; }
    IProjectRepository Projects { get; }
    IAppSettingsRepository AppSettings { get; }

    Task<int> Commit(CancellationToken stoppingToken);
}