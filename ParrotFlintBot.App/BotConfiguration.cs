namespace ParrotFlintBot.App;

public class BotConfiguration
{
    public static readonly string Configuration = "BotConfiguration";

    public string BotToken { get; set; } = string.Empty;
    public bool ThrowPendingUpdates { get; set; } = true;
}