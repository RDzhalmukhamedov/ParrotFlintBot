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
    private System.Timers.Timer? _timer;
    private readonly CronExpression _expression;
    private readonly string _routeKey;

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
        _expression = CronExpression.Parse(cronConfig.Value.Expression);
        _rabbitConfig = rabbitConfig.Value;

        rabbitConfig.Value.PublisherRouteKeys.TryGetValue(RouteKeyNames.ProjectsToCrawl, out var route);
        _routeKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.ProjectsToCrawl : route;
    }

    public async Task StartAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectsToCrawlPublisher Service running.");
        await ScheduleCrawl(stoppingToken);
    }

    public async Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ProjectsToCrawlPublisher Service is stopping.");
        _timer?.Stop();
        _timer?.Dispose();
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _timer?.Dispose();
    }

    private async Task ScheduleCrawl(CancellationToken stoppingToken)
    {
        try
        {
            var next = await GetNextCrawlDate(stoppingToken);
            if (next.HasValue)
            {
                var delay = next.Value - DateTimeOffset.UtcNow;
                // Prevent non-positive values from being passed into Timer
                if (delay.TotalMilliseconds <= 0)
                {
                    // Missed last run, starting immediately
                    await RunCrawl(stoppingToken);
                }
                else
                {
                    _timer = new System.Timers.Timer(delay.TotalMilliseconds);
                    _timer.Elapsed += async (sender, args) =>
                    {
                        // Reset and dispose timer
                        _timer.Dispose();
                        _timer = null;
                        await RunCrawl(stoppingToken);
                    };
                    _timer.Start();
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Scheduling of crawl failed with exception {Exception}", ex);
            
        }
    }

    private async Task RunCrawl(CancellationToken stoppingToken)
    {
        if (!stoppingToken.IsCancellationRequested)
        {
            await ProcessCrawl(stoppingToken);
        }

        if (!stoppingToken.IsCancellationRequested)
        {
            // Reschedule next
            await ScheduleCrawl(stoppingToken);
        }
    }

    private async Task ProcessCrawl(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crawl process has started.");
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
                await db.AppSettings.UpdateCrawlDate(DateTime.UtcNow, stoppingToken);
                await db.Commit(stoppingToken);
                var projects = await db.Projects.GetAllProjectsInfo(stoppingToken);
                if (projects.Any())
                {
                    _publisher.PushMessage(_routeKey, JsonSerializer.Serialize(projects), _rabbitConfig.MessageTTL);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Crawl process failed with exception {Exception}", ex);
        }
    }

    private async Task<DateTimeOffset?> GetNextCrawlDate(CancellationToken stoppingToken)
    {
        using (var scope = _serviceProvider.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<IKSCrawlerUnitOfWork>();
            var lastCrawlDate = await db.AppSettings.GetLastCrawlDate(stoppingToken);
            var next = _expression.GetNextOccurrence(new DateTimeOffset(lastCrawlDate), TimeZoneInfo.Utc);
            return next;
        }
    }
}