using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Abstract;

public interface IUpdatesManagerService
{
    Task<bool> ProcessNewUpdates(List<ProjectInfo> updatesInfo, CancellationToken stoppingToken);
}