using System.Text.RegularExpressions;

namespace DelightsBoutique.Application.Common;

public static class SlugGenerator
{
    public static string FromName(string name)
    {
        var slug = Regex.Replace(name.Trim().ToLowerInvariant(), @"[^a-z0-9]+", "-").Trim('-');
        return string.IsNullOrEmpty(slug) ? Guid.NewGuid().ToString("N")[..8] : slug;
    }
}
