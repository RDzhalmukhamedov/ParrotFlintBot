using ParrotFlintBot.DB.Abstract;

namespace ParrotFlintBot.DB;

public class KSCrawlerUnitOfWork : IKSCrawlerUnitOfWork
{
    private bool _isDisposed;
    private readonly KSCrawlerContext _context;

    public IUserRepository Users { get; }

    public IProjectRepository Projects { get; }

    public IAppSettingsRepository AppSettings { get; }

    public KSCrawlerUnitOfWork(KSCrawlerContext ksCrawlerContext,
        IUserRepository userRepository,
        IProjectRepository projectRepository,
        IAppSettingsRepository appSettingsRepository)
    {
        _context = ksCrawlerContext;

        Users = userRepository;
        Projects = projectRepository;
        AppSettings = appSettingsRepository;
    }

    public Task<int> Commit(CancellationToken stoppingToken)
    {
        return _context.SaveChangesAsync(stoppingToken);
    }

    public void Dispose()
    {
        if (_isDisposed) return;
        _isDisposed = true;
        _context.Dispose();
    }
}