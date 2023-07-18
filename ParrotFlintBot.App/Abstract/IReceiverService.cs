namespace ParrotFlintBot.App.Abstract;

public interface IReceiverService
{
    Task Receive(CancellationToken stoppingToken);
}