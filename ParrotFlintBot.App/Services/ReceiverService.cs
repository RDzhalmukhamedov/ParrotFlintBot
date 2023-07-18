using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ParrotFlintBot.App.Abstract;
using Telegram.Bot;

namespace ParrotFlintBot.App.Services;

public class ReceiverService : ReceiverServiceBase<TelegramUpdateHandler>
{
    public ReceiverService(
        ITelegramBotClient botClient,
        TelegramUpdateHandler updateHandler,
        ILogger<ReceiverServiceBase<TelegramUpdateHandler>> logger,
        IOptions<BotConfiguration> config)
        : base(botClient, updateHandler, logger, config)
    {
    }
}