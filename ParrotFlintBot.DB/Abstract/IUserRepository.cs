using ParrotFlintBot.Domain;

namespace ParrotFlintBot.DB.Abstract;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByChatId(long chatId, CancellationToken stoppingToken, bool includeProjects = false);

    Task<User> CreateIfNotExist(long chatId, CancellationToken stoppingToken);

    Task<List<User>> GetUsersWithSubscriptions(CancellationToken stoppingToken);
}