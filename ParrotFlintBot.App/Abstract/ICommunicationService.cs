using ParrotFlintBot.Shared;
using Telegram.Bot.Types;

namespace ParrotFlintBot.App.Abstract;

public interface ICommunicationService
{
    Task<Message> RequestManageSubscription(long chatId, string link, UserActionType actionType, CancellationToken stoppingToken);

    Task<Message> RequestProjectsList(Message message, CancellationToken stoppingToken);

    Task SendProjectUpdates(List<UpdatesNotification> updates, CancellationToken stoppingToken);

    Task SendProjectsLists(UpdatesNotification info, CancellationToken stoppingToken);

    Task<Message> SendUsage(Message message, CancellationToken stoppingToken);
}