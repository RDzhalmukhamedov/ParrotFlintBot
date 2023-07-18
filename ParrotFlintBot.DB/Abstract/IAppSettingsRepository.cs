namespace ParrotFlintBot.DB.Abstract;

public interface IAppSettingsRepository
{
    Task<DateTime> GetLastCrawlDate(CancellationToken stoppingToken);

    Task UpdateCrawlDate(DateTime date, CancellationToken stoppingToken);
}