using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ParrotFlintBot.Shared;

public static class Extensions
{
    private static readonly string[] MdSpecialChars = {
        "\\",
        "_",
        "*",
        "[",
        "]",
        "(",
        ")",
        "~",
        "`",
        ">",
        "<",
        "&",
        "#",
        "+",
        "-",
        "=",
        "|",
        "{",
        "}",
        ".",
        "!"
    };

    public static T GetConfiguration<T>(this IServiceProvider serviceProvider)
        where T : class
    {
        var o = serviceProvider.GetService<IOptions<T>>();
        if (o is null)
            throw new ArgumentNullException(nameof(T));

        return o.Value;
    }

    public static string GetProjectSlug(this Uri ksUrl)
    {
        var urlParts = ksUrl.AbsolutePath.Split('/');
        // Path will be like "/projects/{creator-slug}/{project-slug}/..."
        if (urlParts.Length < 4)
        {
            throw new InvalidDataException(ksUrl.OriginalString);
        }

        return urlParts[3];
    }

    public static string GetCreatorSlug(this Uri ksUrl)
    {
        var urlParts = ksUrl.AbsolutePath.Split('/');
        // Path will be like "/projects/{creator-slug}/{project-slug}/..."
        if (urlParts.Length < 4)
        {
            throw new InvalidDataException(ksUrl.OriginalString);
        }

        return urlParts[2];
    }

    public static string GetSiteName(this Uri ksUrl)
    {
        var urlParts = ksUrl.Host.Split('.');
        // Host will be like "www.{site-name}.com or {site-name}.com"
        if (urlParts.Length < 2)
        {
            throw new InvalidDataException(ksUrl.OriginalString);
        }

        return urlParts[^2];
    }

    public static bool IsNullOrEmpty<T>(this IEnumerable<T>? list) where T : class
    {
        return list is null || !list.Any();
    }

    public static string EscapeMdChars(this string message)
    {
        var result = message;
        foreach (var specialChar in MdSpecialChars)
        {
            result = result.Replace(specialChar, $"\\{specialChar}");
        }

        return result;
    }
}