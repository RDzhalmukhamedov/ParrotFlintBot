namespace ParrotFlintBot.Domain;

public static class Extensions
{
    public static string GetUrlToCrawl(this Project project)
    {
        if (project.Site.Equals("kickstarter"))
        {
            return $"https://www.kickstarter.com/projects/{project.CreatorSlug}/{project.ProjectSlug}/posts";
        }
        else
        {
            return $"https://gamefound.com/projects/{project.CreatorSlug}/{project.ProjectSlug}/updates";
        }
    }
    public static string GetUrlForUpdate(this Project project)
    {
        if (project.Site.Equals("kickstarter"))
        {
            return $"https://www.kickstarter.com/projects/{project.CreatorSlug}/{project.ProjectSlug}/posts/{project.LastUpdateId}";
        }
        else
        {
            return $"https://gamefound.com/projects/{project.CreatorSlug}/{project.ProjectSlug}/updates/{project.LastUpdateId}";
        }
    }
}