namespace ParrotFlintBot.DB.Abstract;

public interface IRepository<T> where T : class
{
    Task<List<T>> GetAll(CancellationToken stoppingToken);
    Task<T?> GetById(int id, CancellationToken stoppingToken);
    Task Add(T entity, CancellationToken stoppingToken);
    void Update(T entity);
    Task Delete(int id, CancellationToken stoppingToken);
}