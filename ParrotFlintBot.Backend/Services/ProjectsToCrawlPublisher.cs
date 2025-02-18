using System.Text.Json;
using Cronos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Services;

public class ProjectsToCrawlPublisher : IHostedService, IDisposable
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ProjectsToCrawlPublisher> _logger;
    private readonly RabbitMQPublisher _publisher;
    private readonly RabbitMQConfiguration _rabbitConfig;
    private readonly CronExpression _fullCrawlExpression;
    private readonly CronExpression _simpleCrawlExpression;
    private readonly string _fullCrawlRouteKey;
    private readonly string _simpleCrawlRouteKey;

    private System.Timers.Timer? _fullCrawlTimer;
    private System.Timers.Timer? _simpleCrawlTimer;

    public ProjectsToCrawlPublisher(
        RabbitMQPublisher publisher,
        IServiceProvider serviceProvider,
        ILogger<ProjectsToCrawlPublisher> logger,
        IOptions<CronConfiguration> cronConfig,
        IOptions<RabbitMQConfiguration> rabbitConfig)
    {
        _publisher = publisher;
        _serviceProvider = serviceProvider;
        _logger = logger;
        _fullCrawlExpression = CronExpression.Parse(cronConfig.Value.FullCrawlExpression);
        _simpleCrawlExpression = CronExpression.Parse(cronConfig.Value.LiteCrawlExpression);
        _rabbitConfig = rabbitConfig.Value;

        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsToFullCrawl, out var route);
        _fullCrawlRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.ProjectsToFullCrawl : route;
        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsToSimpleCrawl, out route);
        _simpleCrawlRouteKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.ProjectsToSimpleCrawl : route;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectsToCrawlPublisher Service running.");
        await ScheduleSimpleCrawl(stoppingToken);
        await ScheduleFullCrawl(stoppingToken);
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectsToCrawlPublisher Service is stopping.");

        _fullCrawlTimer?.Stop();
        _fullCrawlTimer?.Dispose();

        _simpleCrawlTimer?.Stop();
        _simpleCrawlTimer?.Dispose();

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _fullCrawlTimer?.Dispose();
        _simpleCrawlTimer?.Dispose();
    }

    private async Task RunCrawl(Func<CancellationToken, Task> processCrawl,
        Func<CancellationToken, Task> scheduleCrawl, CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            await processCrawl(stoppingToken);
        }

        if (!stoppingToken.IsCancellationRequested)
        {
            // Reschedule next
            await scheduleCrawl(stoppingToken);
        }
    }

    private Task ScheduleSimpleCrawl(CancellationToken stoppingToken)
    {
        try
        {
            var nextDate = _simpleCrawlExpression.GetNextOccurrence(new DateTimeOffset(DateTime.UtcNow), TimeZoneInfo.Utc);
            if (nextDate.HasValue)
            {
                var delay = nextDate.Value - DateTimeOffset.UtcNow;
                _simpleCrawlTimer = new System.Timers.Timer(delay.TotalMilliseconds);
                _simpleCrawlTimer.Elapsed += async (sender, args) =>
                {
                    // Reset and dispose timer
                    _simpleCrawlTimer.Dispose();
                    _simpleCrawlTimer = null;
                    await RunCrawl(ProcessSimpleCrawl, ScheduleSimpleCrawl, stoppingToken);
                };
                _simpleCrawlTimer.Start();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Scheduling of simple crawl failed with exception {Exception}", ex);

        }

        return Task.CompletedTask;
    }

    private async Task ProcessSimpleCrawl(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Simple crawl process has started.");
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
                var projects = await db.Projects.GetAllProjectsInfoForSimpleCrawl(stoppingToken);
                if (projects.Any())
                {
                    _publisher.PushMessage(_simpleCrawlRouteKey, projects, _rabbitConfig.MessageTTL);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Crawl process failed with exception {Exception}", ex);
        }
    }

    private async Task ScheduleFullCrawl(CancellationToken stoppingToken)
    {
        try
        {
            var nextDate = await GetNextFullCrawlDate(stoppingToken);
            if (nextDate.HasValue)
            {
                var delay = nextDate.Value - DateTimeOffset.UtcNow;
                // Prevent non-positive values from being passed into Timer
                if (delay.TotalMilliseconds <= 0)
                {
                    // Missed last run, starting immediately
                    await RunCrawl(ProcessFullCrawl, ScheduleFullCrawl, stoppingToken);
                }
                else
                {
                    _fullCrawlTimer = new System.Timers.Timer(delay.TotalMilliseconds);
                    _fullCrawlTimer.Elapsed += async (sender, args) =>
                    {
                        // Reset and dispose timer
                        _fullCrawlTimer.Dispose();
                        _fullCrawlTimer = null;
                        await RunCrawl(ProcessFullCrawl, ScheduleFullCrawl, stoppingToken);
                    };
                    _fullCrawlTimer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Scheduling of full crawl failed with exception {Exception}", ex);
            
        }
    }

    private async Task ProcessFullCrawl(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Full crawl process has started.");
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
                await db.AppSettings.UpdateCrawlDate(DateTime.UtcNow, stoppingToken);
                await db.Commit(stoppingToken);
                var projects = await db.Projects.GetAllProjectsInfoForFullCrawl(stoppingToken);
                if (projects.Any())
                {
                    _publisher.PushMessage(_fullCrawlRouteKey, projects, _rabbitConfig.MessageTTL);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Crawl process failed with exception {Exception}", ex);
        }
    }

    private async Task<DateTimeOffset?> GetNextFullCrawlDate(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
            var lastCrawlDate = await db.AppSettings.GetLastCrawlDate(stoppingToken);
            var next = _fullCrawlExpression.GetNextOccurrence(new DateTimeOffset(lastCrawlDate), TimeZoneInfo.Utc);
            return next;
        }
    }
}