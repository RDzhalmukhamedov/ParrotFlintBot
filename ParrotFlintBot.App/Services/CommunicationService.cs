using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.App.Abstract;
using ParrotFlintBot.RabbitMQ;
using ParrotFlintBot.Shared;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace ParrotFlintBot.App.Services;

public class CommunicationService : ICommunicationService
{
    private readonly RabbitMQPublisher _publisher;
    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<CommunicationService> _logger;
    private readonly RabbitMQConfiguration _config;
    private readonly string _routeKey;

    public CommunicationService(
        ITelegramBotClient botClient,
        RabbitMQPublisher publisher,
        ILogger<CommunicationService> logger,
        IOptions<RabbitMQConfiguration> config)
    {
        _logger = logger;
        _botClient = botClient;
        _publisher = publisher;
        _config = config.Value;
        _config.PublisherRouteKeys.TryGetValue(RouteKeyNames.UserActions, out var route);
        _routeKey = string.IsNullOrWhiteSpace(route) ? RouteKeyNames.UserActions : route;
    }

    public async Task<Message> RequestManageSubscription(
        long chatId,
        string url,
        UserActionType actionType,
        CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called subscribing to {url}", url);

        string message = "Некорректная ссылка, попробуйте другую";
        bool result = TryCreateUrl(url, out var link);

        if (result)
        {
            link = await UnshortenLink(link!, stoppingToken);
            if (link is not null)
            {
                var successMessage = actionType is UserActionType.Subscribe
                    ? $"Вы успешно подписались на обновления для {link.GetLeftPart(UriPartial.Path)}"
                    : $"Вы успешно отписались от получения обновлений для {link.GetLeftPart(UriPartial.Path)}";
                
                _publisher.PushMessage(_routeKey, new UserActionInfo(chatId, link, actionType), _config.MessageTTL);
                // TODO Use events instead (later)
                var ack = _publisher.WaitForAck();
                message = ack ? successMessage : "Что-то пошло не так, попробуйте позже";
            }
        }

        return await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: message,
            disableWebPagePreview: true,
            cancellationToken: stoppingToken);
    }

    public async Task<Message> RequestProjectsList(Message message, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called getting list of projects");
        _publisher.PushMessage(_routeKey, new UserActionInfo(message.Chat.Id, projectLink: null, UserActionType.List), _config.MessageTTL);
        return await Task.FromResult(message);
    }

    public async Task SendProjectUpdates(List<UpdatesNotification> updates, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sending list of new crawled updates.");
        var notifications = updates.Select(update => SendUpdateMessages(update, stoppingToken));
        await Task.WhenAll(notifications);
    }

    public async Task SendProjectsLists(UpdatesNotification info, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Sending list of subscribed projects.");

        if (info.Updates.IsNullOrEmpty())
        {
            await _botClient.SendTextMessageAsync(
                chatId: info.ChatId,
                text: "У вас пока нет подписок на проекты\\.",
                parseMode: ParseMode.MarkdownV2,
                disableNotification: true,
                disableWebPagePreview: true,
                cancellationToken: stoppingToken);
        }
        else
        {
            var notifications = info.Updates.Chunk(10)
                .Select(chunk => SendProjectListMessage(info.ChatId, chunk, stoppingToken));
            await Task.WhenAll(notifications);
        }
    }

    public async Task<Message> SendUsage(Message message, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Called usage message.");
        const string usage = "Для слежения доступны проекты на Kickstarter и Gamefound." +
                             " Для подписки можно отправлять короткие ссылки." +
                             "\nКоманды:" +
                             "\n/s {Сссылка} – подписаться на проект по ссылке" +
                             "\n/u {Ссылка} – отписаться от проека по ссылке" +
                             "\n/list – список проектов, на которые вы подписаны";

        return await _botClient.SendTextMessageAsync(
            chatId: message.Chat.Id,
            text: usage,
            cancellationToken: stoppingToken);
    }

    private async Task<Message[]> SendUpdateMessages(UpdatesNotification update, CancellationToken stoppingToken)
    {
        StringBuilder sb = new StringBuilder();
        var updateMessages = update.Updates.Select(u =>
        {
            if (u.PrevStatus is ProjectStatus.NotTracked)
            {
                sb.Append("*НОВОЕ* ");
            }

            sb.Append(
                $"Для *{u.ProjectName.EscapeMdChars()}* вышло *{u.UpdatesCount - u.PrevUpdatesCount}* новых апдейтов\\.");
            sb.Append(
                $"\nПоследний: [{(string.IsNullOrWhiteSpace(u.LastUpdateTitle) ? u.Link : u.LastUpdateTitle.EscapeMdChars())}]({u.Link})");
            var message = sb.ToString();
            sb.Clear();
            return _botClient.SendTextMessageAsync(
                chatId: update.ChatId,
                text: message,
                parseMode: ParseMode.MarkdownV2,
                disableNotification: true,
                disableWebPagePreview: true,
                cancellationToken: stoppingToken);
        });
        return await Task.WhenAll(updateMessages);
    }

    private async Task<Message> SendProjectListMessage(long chatId, ProjectInfo[] projects, CancellationToken stoppingToken)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var project in projects)
        {
            sb.Append(project.Link.Contains("kickstarter.com") ? "\U0001F1F0" : "\U0001F1EC");
            sb.Append("\U0001F3F4");
            if (project.Status is ProjectStatus.NotTracked)
            {
                sb.Append("*НЕ ОТСЛЕЖЕНО* ");
            }

            sb.Append(
                $"[{project.ProjectName.EscapeMdChars()}]({project.Link}) {project.UpdatesCount} апдейтов\n");
        }
        return await _botClient.SendTextMessageAsync(
            chatId: chatId,
            text: sb.ToString(),
            parseMode: ParseMode.MarkdownV2,
            disableNotification: true,
            disableWebPagePreview: true,
            cancellationToken: stoppingToken);
    }

    private async Task<Uri?> UnshortenLink(Uri url, CancellationToken stoppingToken)
    {
        try
        {
            using (var client = new HttpClient())
            {
                // TODO Use user-agent generator (later)
                client.DefaultRequestHeaders.UserAgent.ParseAdd(
                    "Mozilla/5.0 (Windows NT 10.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/111.0.0.0 Safari/537.36");
                var response = await client.GetAsync(url, stoppingToken);
                var realLink = response.RequestMessage?.RequestUri;

                if (realLink is not null)
                {
                    var parts = realLink.Host.Split('.');
                    var site = parts[^2];
                    var isValidLink = site.Equals("kickstarter") || site.Equals("gamefound");
                    if (isValidLink) return realLink;
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError("Unshorten failed with exception: {Exception}", ex);
        }

        return null;
    }

    private bool TryCreateUrl(string url, out Uri? link)
    {
        bool result = Uri.TryCreate(url, UriKind.Absolute, out link)
                      && (link.Scheme == Uri.UriSchemeHttp || link.Scheme == Uri.UriSchemeHttps);
        return result;
    }
}