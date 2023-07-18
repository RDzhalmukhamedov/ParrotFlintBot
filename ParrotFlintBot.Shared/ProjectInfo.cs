#pragma warning disable CS8618
namespace ParrotFlintBot.Shared;

public class ProjectInfo
{
    public int ProjectId { get; set; }
    
    public string ProjectName { get; set; }
    
    public string Link { get; set; }
    
    public ProjectStatus Status { get; set; }
    
    public ProjectStatus PrevStatus { get; set; }
    
    public long? LastUpdateId { get; set; }
    
    public string? LastUpdateTitle { get; set; }
    
    public short UpdatesCount { get; set; }
    
    public short PrevUpdatesCount { get; set; }
}