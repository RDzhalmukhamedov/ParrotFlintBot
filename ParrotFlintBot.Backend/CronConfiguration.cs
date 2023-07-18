namespace ParrotFlintBot.Backend;

public class CronConfiguration
{
    public static readonly string Configuration = "CronConfig";
    
    public string Expression { get; set; } = "0 12 * * *";
}