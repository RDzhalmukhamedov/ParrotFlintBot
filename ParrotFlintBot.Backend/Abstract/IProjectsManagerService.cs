using ParrotFlintBot.Shared;

namespace ParrotFlintBot.Backend.Abstract;

public interface IProjectsManagerService
{
    Task<bool> ProcessNewUpdates(List<ProjectInfo> updatesInfo, CancellationToken stoppingToken);
    
    Task<bool> ProcessProjectsList(long chatId, CancellationToken stoppingToken);
}