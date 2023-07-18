namespace ParrotFlintBot.RabbitMQ;

public class RabbitMQConfiguration
{
    public static readonly string Configuration = "RabbitMQConfiguration";
    
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 15672;
    public string Username { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public bool EnableAckConfirmation { get; set; } = false;
    public int MessageTTL { get; set; } = 30000;
    public Dictionary<string, string> ListenerRouteKeys { get; set; } = new();
    public Dictionary<string, string> PublisherRouteKeys { get; set; } = new();
}