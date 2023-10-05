using Microsoft.EntityFrameworkCore;
using ParrotFlintBot.DB.Abstract;
using ParrotFlintBot.Domain;

namespace ParrotFlintBot.DB;

public class UserRepository : IUserRepository
{
    private readonly KSCrawlerContext _context;

    public UserRepository(KSCrawlerContext ksCrawlerContext)
    {
        _context = ksCrawlerContext;
    }

    public async Task<User?> GetById(int id, CancellationToken stoppingToken)
    {
        return await _context.Users.FindAsync(id, stoppingToken);
    }

    public async Task<User?> GetByChatId(long chatId, CancellationToken stoppingToken, bool includeProjects = false)
    {
        var users = includeProjects 
            ? _context.Users.Include(u => u.Projects)
            : _context.Users.AsQueryable();
        return await users.FirstOrDefaultAsync(u => u.ChatId == chatId, stoppingToken);
    }

    public async Task<List<User>> GetAll(CancellationToken stoppingToken)
    {
        return await _context.Users.ToListAsync(stoppingToken);
    }

    public async Task<List<User>> GetUsersWithSubscriptions(CancellationToken stoppingToken)
    {
        return await _context.Users
            .Include(u => u.Projects)
            .Where(u => u.Projects.Count > 0)
            .ToListAsync(stoppingToken);
    }

    public async Task Add(User user, CancellationToken stoppingToken)
    {
        await _context.Users.AddAsync(user, stoppingToken);
    }

    public async Task<User> CreateIfNotExist(long chatId, CancellationToken stoppingToken)
    {
        var user = await GetByChatId(chatId, stoppingToken, true);
        if (user is null)
        {
            user = new User() { ChatId = chatId };
            await Add(user, stoppingToken);
        }

        return user;
    }

    public void Update(User user)
    {
        _context.Users.Update(user);
    }

    public async Task Delete(int id, CancellationToken stoppingToken)
    {
        var user = await GetById(id, stoppingToken);
        if (user is not null)
        {
            _context.Users.Remove(user);
        }
    }
}