using Microsoft.EntityFrameworkCore;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;

namespace ParrotFlintBot.DB;

public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly KSCrawlerContext _context;

    public AppSettingsRepository(KSCrawlerContext ksCrawlerContext)
    {
        _context = ksCrawlerContext;
    }
    
    public async Task<DateTime> GetLastCrawlDate(CancellationToken stoppingToken)
    {
        return await _context.AppSettings.OrderBy(s => s.Id)
            .Select(s => s.DateOfLastCrawl).LastOrDefaultAsync(stoppingToken);
    }

    public async Task UpdateCrawlDate(DateTime date, CancellationToken stoppingToken)
    {
        var newEntry = new AppSettings()
        {
            DateOfLastCrawl = date,
        };
        await _context.AppSettings.AddAsync(newEntry, stoppingToken);
    }
}