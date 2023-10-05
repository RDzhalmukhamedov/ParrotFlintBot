using ParrotFlintBot.Shared;

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

    public static Project? ContainsEqualProject(this IEnumerable<Project> projects, Uri projectUrl)
    {
        var projectSlug = projectUrl.GetProjectSlug();
        var creatorSlug = projectUrl.GetCreatorSlug();
        var site = projectUrl.GetSiteName();
        return projects.FirstOrDefault(project =>
            project.ProjectSlug.Equals(projectSlug, StringComparison.CurrentCultureIgnoreCase)
            && project.CreatorSlug.Equals(creatorSlug, StringComparison.CurrentCultureIgnoreCase)
            && project.Site.Equals(site, StringComparison.CurrentCultureIgnoreCase));
    }
}