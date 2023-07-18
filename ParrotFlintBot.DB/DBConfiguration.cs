namespace ParrotFlintBot.DB;

public class DbConfiguration
{
    public static readonly string Configuration = "DBConfiguration";
    
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string DbName { get; set; } = "postgres";
    public string Username { get; set; } = "postgres";
    public string Password { get; set; } = string.Empty;
}