namespace ParrotFlintBot.Shared;

public class UpdatesNotification
{
    public long ChatId { get; set; }

    public List<ProjectInfo> Updates { get; set; } = new();
}