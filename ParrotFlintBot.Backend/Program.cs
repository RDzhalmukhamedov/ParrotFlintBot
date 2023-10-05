using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using ParrotFlintBot.Backend;
using ParrotFlintBot.Backend.Abstract;
using ParrotFlintBot.Backend.Services;
using ParrotFlintBot.DB;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.ClearProviders();
        logging.SetMinimumLevel(LogLevel.Trace);
        LogManager.Setup().LoadConfigurationFromAppSettings();
    })
    .UseNLog()
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppConfig>(context.Configuration.GetSection(AppConfig.Configuration));
        services.Configure<DbConfiguration>(context.Configuration.GetSection(DbConfiguration.Configuration));
        services.Configure<RabbitMQConfiguration>(
            context.Configuration.GetSection(RabbitMQConfiguration.Configuration));
        services.Configure<CronConfiguration>(context.Configuration.GetSection(CronConfiguration.Configuration));

        services.AddDbContext<KSCrawlerContext>();

        services.AddTransient<IUserRepository, UserRepository>();
        services.AddTransient<IProjectRepository, ProjectRepository>();
        services.AddTransient<IAppSettingsRepository, AppSettingsRepository>();
        services.AddTransient<IKSCrawlerUnitOfWork, KSCrawlerUnitOfWork>();

        services.AddScoped<ISubscriptionService, SubscriptionService>();
        services.AddScoped<IProjectsManagerService, ProjectsManagerService>();

        services.AddHostedService<UserActionsListener>();
        services.AddHostedService<CrawledUpdatesListener>();
        services.AddHostedService<ProjectsToCrawlPublisher>();

        services.AddSingleton<RabbitMQPublisher>();
    })
    .Build();

await host.RunAsync();