using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.RSSReader.Services;
using ParrotFlintBot.Shared;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

IHost host = Host.CreateDefaultBuilder(args)
	.ConfigureLogging(logging =>
	{
		//logging.ClearProviders();
		logging.SetMinimumLevel(LogLevel.Trace);
		//LogManager.Setup().LoadConfigurationFromAppSettings();
	})
	//.UseNLog()
	.ConfigureServices((context, services) =>
	{
		services.Configure<AppConfig>(context.Configuration.GetSection(AppConfig.Configuration));
		services.Configure<RabbitMQConfiguration>(context.Configuration.GetSection(RabbitMQConfiguration.Configuration));

        services.AddHostedService<ProjectsToCrawlListener>();

        services.AddSingleton<RabbitMQPublisher>();
	})
	.Build();

await host.RunAsync();