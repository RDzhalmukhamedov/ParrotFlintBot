using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ParrotFlintBot.App;
using ParrotFlintBot.App.Abstract;
using ParrotFlintBot.App.Services;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;
using Telegram.Bot;

IHost host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.Configure<AppConfig>(context.Configuration.GetSection(AppConfig.Configuration));
        services.Configure<BotConfiguration>(context.Configuration.GetSection(BotConfiguration.Configuration));
        services.Configure<RabbitMQConfiguration>(
            context.Configuration.GetSection(RabbitMQConfiguration.Configuration));

        services.AddHttpClient("telegram_bot_client")
            .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
            {
                BotConfiguration botConfig = sp.GetConfiguration<BotConfiguration>();
                TelegramBotClientOptions options = new(botConfig.BotToken);
                return new TelegramBotClient(options, httpClient);
            });

        services.AddScoped<TelegramUpdateHandler>();
        services.AddScoped<ReceiverService>();
        services.AddScoped<ICommunicationService, CommunicationService>();

        services.AddHostedService<PollingService>();
        services.AddHostedService<UpdatesNotificationListener>();
        services.AddHostedService<ProjectsListListener>();
        services.AddSingleton<RabbitMQPublisher>();
    })
    .Build();

await host.RunAsync();