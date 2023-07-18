namespace ParrotFlintBot.Domain;

public static class Extensions
{
    public static string GetUrlToCrawl(this Project project)
    {
        return $"https://www.kickstarter.com/projects/{project.CreatorSlug}/{project.ProjectSlug}/posts";
    }
    public static string GetUrlForUpdate(this Project project)
    {
        return $"https://www.kickstarter.com/projects/{project.CreatorSlug}/{project.ProjectSlug}/posts/{project.LastUpdateId}";
    }
}