using System.Text.Json.Serialization;

namespace ParrotFlintBot.Shared;

public class UserActionInfo
{
    public UserActionType Type { get; set; }
    
    public long? ChatId { get; set; }

    public string UserId { get; set; }

    public Uri? ProjectLink { get; }

    public UserActionInfo(long chatId, string userId, string url, UserActionType type = UserActionType.Subscribe)
    {
        ChatId = chatId;
        UserId = userId;
        ProjectLink = new Uri(url);
        Type = type;
    }

    [JsonConstructor]
    public UserActionInfo(long chatId, string userId, Uri? projectLink, UserActionType type = UserActionType.Subscribe)
    {
        ChatId = chatId;
        UserId = userId;
        ProjectLink = projectLink;
        Type = type;
    }

    public override string ToString()
    {
        return $"\'{Type}\' for {UserId??"Unknown"}({ChatId}) and {ProjectLink}";
    }
}