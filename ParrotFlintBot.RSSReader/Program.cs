using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.RSSReader.Services;
using ParrotFlintBot.Shared;

using LogLevel = Microsoft.Extensions.Logging.LogLevel;

var builder = WebApplication.CreateBuilder(args);
builder.Logging.SetMinimumLevel(LogLevel.Trace);

builder.Host
    .ConfigureServices((context, services) =>
	{
		services.Configure<AppConfig>(context.Configuration.GetSection(AppConfig.Configuration));
		services.Configure<RabbitMQConfiguration>(context.Configuration.GetSection(RabbitMQConfiguration.Configuration));

        services.AddHostedService<ProjectsToCrawlListener>();

        services.AddSingleton<RabbitMQPublisher>();

        services.AddControllers();

        services.AddHealthChecks();
    });

var app = builder.Build();

app.MapControllerRoute(name: "default", pattern: "{controller=App}/{action=Index}/{id?}");
app.MapHealthChecks("/healthz");

await app.RunAsync();