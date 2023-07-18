using System.Text.Json.Serialization;

namespace ParrotFlintBot.Shared;

public class UserActionInfo
{
    public UserActionType Type { get; set; }
    
    public long ChatId { get; set; }
    
    public Uri? ProjectLink { get; }

    public UserActionInfo(long chatId, string url, UserActionType type = UserActionType.Subscribe)
    {
        ChatId = chatId;
        ProjectLink = new Uri(url);
        Type = type;
    }

    [JsonConstructor]
    public UserActionInfo(long chatId, Uri? projectLink, UserActionType type = UserActionType.Subscribe)
    {
        ChatId = chatId;
        ProjectLink = projectLink;
        Type = type;
    }

    public override string ToString()
    {
        return $"\'{Type}\' for {ChatId} and {ProjectLink}";
    }
}