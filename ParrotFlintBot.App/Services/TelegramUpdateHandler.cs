using Microsoft.Extensions.Logging;
using ParrotFlintBot.App.Abstract;
using ParrotFlintBot.Shared;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;

namespace ParrotFlintBot.App.Services;

public class TelegramUpdateHandler : IUpdateHandler
{
    private readonly ILogger<TelegramUpdateHandler> _logger;
    private readonly ICommunicationService _communication;

    public TelegramUpdateHandler(ICommunicationService communicationService, ILogger<TelegramUpdateHandler> logger)
    {
        _communication = communicationService;
        _logger = logger;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient _, Update update, CancellationToken stoppingToken)
    {
        var handler = update switch
        {
            { Message: { } message }       => BotOnMessageReceived(message, stoppingToken),
            { EditedMessage: { } message } => BotOnMessageReceived(message, stoppingToken),
            _                              => UnknownUpdateHandler(update)
        };

        await handler;
    }

    public async Task HandlePollingErrorAsync(ITelegramBotClient botClient, Exception exception,
        CancellationToken stoppingToken)
    {
        var errorMessage = exception switch
        {
            ApiRequestException apiRequestException =>
                $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        _logger.LogInformation("HandleError: {ErrorMessage}", errorMessage);

        if (exception is RequestException)
            await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
    }

    private async Task BotOnMessageReceived(Message message, CancellationToken stoppingToken)
    {
        _logger.LogInformation("Receive message type: {MessageType} from {ChatId}", message.Type, message.Chat.Id);
        if (message.Text is not { } messageText)
            return;

        var words = messageText.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var action = words[0] switch
        {
            "/sub" or "/s"   => _communication.RequestManageSubscription(message.Chat.Id,
                                        words.Length > 1 ? words[1] : string.Empty,
                                        UserActionType.Subscribe,
                                        stoppingToken),
            "/unsub" or "/u" => _communication.RequestManageSubscription(message.Chat.Id,
                                        words.Length > 1 ? words[1] : string.Empty,
                                        UserActionType.Unsubscribe,
                                        stoppingToken),
            "/projects" or "/list" => _communication.RequestProjectsList(message, stoppingToken),
            _                => _communication.SendUsage(message, stoppingToken)
        };
        Message sentMessage = await action;
        _logger.LogInformation("The message was sent with id: {SentMessageId}", sentMessage.MessageId);
    }

// #pragma warning disable IDE0060 // Remove unused parameter
// #pragma warning disable RCS1163 // Unused parameter.
    private Task UnknownUpdateHandler(Update update)
// #pragma warning restore RCS1163 // Unused parameter.
// #pragma warning restore IDE0060 // Remove unused parameter
    {
        _logger.LogInformation("Unknown update type: {UpdateType}", update.Type);
        return Task.CompletedTask;
    }
}