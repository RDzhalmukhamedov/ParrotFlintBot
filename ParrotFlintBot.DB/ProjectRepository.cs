using Microsoft.EntityFrameworkCore;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;
using ParrotFlintBot.Shared;

namespace ParrotFlintBot.DB;

public class ProjectRepository : IProjectRepository
{
    private readonly KSCrawlerContext _context;

    public ProjectRepository(KSCrawlerContext ksCrawlerContext)
    {
        _context = ksCrawlerContext;
    }

    public async Task<Project?> GetById(int id, CancellationToken stoppingToken)
    {
        return await _context.Projects.FindAsync(id, stoppingToken);
    }

    public async Task<Project?> GetByProjectSlug(string projectSlug, CancellationToken stoppingToken)
    {
        return await _context.Projects.FirstOrDefaultAsync(p => EF.Functions.Like(projectSlug, p.ProjectSlug),
            stoppingToken);
    }

    public async Task<List<Project>> GetAll(CancellationToken stoppingToken)
    {
        return await _context.Projects.ToListAsync(stoppingToken);
    }

    public async Task<List<ProjectInfo>> GetAllProjectsInfo(CancellationToken stoppingToken)
    {
        var result = _context.Projects
            .Where(p => p.Users.Count > 0)
            .Select(p => new ProjectInfo()
            {
                ProjectId = p.Id,
                Status = p.Status,
                Link = p.GetUrlToCrawl(),
                ProjectName = p.Name,
                PrevStatus = p.Status,
                UpdatesCount = p.UpdatesCount,
                PrevUpdatesCount = p.PrevUpdatesCount
            });
        return await result.ToListAsync(stoppingToken);
    }

    public async Task Add(Project project, CancellationToken stoppingToken)
    {
        await _context.Projects.AddAsync(project, stoppingToken);
    }

    public async Task<Project> CreateIfNotExist(string projectSlug, string creatorSlug, CancellationToken stoppingToken)
    {
        var project = await GetByProjectSlug(projectSlug, stoppingToken);
        if (project is null)
        {
            project = new Project()
            {
                Name = $"{creatorSlug}/{projectSlug}",
                ProjectSlug = projectSlug,
                CreatorSlug = creatorSlug
            };
            await Add(project, stoppingToken);
        }

        return project;
    }

    public void Update(Project project)
    {
        _context.Projects.Update(project);
    }

    public Task Update(ProjectInfo info)
    {
        var project = new Project()
        {
            Id = info.ProjectId,
            Name = info.ProjectName,
            Status = info.Status,
            LastUpdateId = info.LastUpdateId,
            LastUpdateTitle = info.LastUpdateTitle,
            UpdatesCount = info.UpdatesCount,
            PrevUpdatesCount = info.PrevUpdatesCount,
        };
        _context.Projects.Attach(project);
        _context.Projects.Entry(project).Property(p => p.Name).IsModified = true;
        _context.Projects.Entry(project).Property(p => p.Status).IsModified = true;
        _context.Projects.Entry(project).Property(p => p.LastUpdateId).IsModified = true;
        _context.Projects.Entry(project).Property(p => p.LastUpdateTitle).IsModified = true;
        _context.Projects.Entry(project).Property(p => p.UpdatesCount).IsModified = true;
        _context.Projects.Entry(project).Property(p => p.PrevUpdatesCount).IsModified = true;

        return Task.CompletedTask;
    }

    public async Task BulkUpdate(List<ProjectInfo> updates, CancellationToken stoppingToken)
    {
        await Task.WhenAll(updates.Select(update =>
            stoppingToken.IsCancellationRequested ? Task.CompletedTask : Update(update)));
    }

    public async Task Delete(int id, CancellationToken stoppingToken)
    {
        var project = await GetById(id, stoppingToken);
        if (project is not null)
        {
            _context.Projects.Remove(project);
        }
    }
}