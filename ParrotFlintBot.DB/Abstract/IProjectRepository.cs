using ParrotFlintBot.Domain;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.DB.Abstract;

public interface IProjectRepository : IRepository<Project>
{
    Task<List<ProjectInfo>> GetAllProjectsInfo(CancellationToken stoppingToken);

    Task<Project?> GetByProjectSlug(string projectSlug, CancellationToken stoppingToken);

    Task<Project> CreateIfNotExist(string projectSlug, string creatorSlug, CancellationToken stoppingToken);

    Task Update(ProjectInfo info);

    Task BulkUpdate(List<ProjectInfo> updates, CancellationToken stoppingToken);
}