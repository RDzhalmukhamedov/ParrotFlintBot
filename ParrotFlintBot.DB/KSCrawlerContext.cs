using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using ParrotFlintBot.Domain;

namespace ParrotFlintBot.DB;

public sealed class KSCrawlerContext : DbContext
{
    private readonly DbConfiguration _config;

    public DbSet<Project> Projects { get; set; } = null!;

    public DbSet<User> Users { get; set; } = null!;

    public DbSet<AppSettings> AppSettings { get; set; } = null!;

    public KSCrawlerContext(IOptions<DbConfiguration> configuration)
    {
        _config = configuration.Value;
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql($"Host={_config.Host};Port={_config.Port};Database={_config.DbName};"
                                 + $"Username={_config.Username};Password={_config.Password}");
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AppSettings>()
            .Property(p => p.DateOfLastCrawl)
            .HasConversion
            (
                src => src.Kind == DateTimeKind.Utc ? src : DateTime.SpecifyKind(src, DateTimeKind.Utc),
                dst => dst.Kind == DateTimeKind.Utc ? dst : DateTime.SpecifyKind(dst, DateTimeKind.Utc)
            );
        modelBuilder.Entity<AppSettings>().HasData(new AppSettings()
        {
            Id = 1,
            DateOfLastCrawl = DateTime.UtcNow
        });
    }
}