using LogoSlideMaker.Configure;

namespace LogoSlideMaker.Lib.Configure;

/// <summary>
/// Manage including other definitions into in a defintition
/// </summary>
public static class IncludeEngine
{
    /// <summary>
    /// Include logos from the <paramref name="source"/> definition into this definition
    /// </summary>
    /// <remarks>
    /// Source logos will be stripped of their text width. To supply a text width for an
    /// included logo, simply define this logo in the target with ONLY a text width
    /// </remarks>
    /// <param name="target">Where the logos will be merged into</param>
    /// <param name="source">Where the logos will come from</param>
    public static void IncludeLogosFrom(this Definition target, Definition source)
    {
        // Divide existing logos into those which include primary properites (path or name),
        // and those which only include secondary properties
        var existing = target.Logos.GroupBy(x => x.Value.HasPrimaryProperties());
        var primary = existing.Where(x => x.Key == true).SelectMany(x => x.Select(y => y.Key)).ToHashSet();
        var overrides = existing
            .Where(x => x.Key == false)
            .SelectMany(x => x)
            .ToDictionary(x => x.Key, y => new Logo() { TextWidth = y.Value.TextWidth });

        // Extract the logos from the source, excluding existing logos, and stripping out the text width
        var logos = source.Logos.Select(x => (x.Key, Value: x.Value with { TextWidth = null })).Where(x => !primary.Contains(x.Key));

        // Merge those logos in
        foreach(var logo in logos)
        {
            if (overrides.TryGetValue(logo.Key, out var o))
            {
                target.Logos[logo.Key] = logo.Value with { TextWidth = o.TextWidth };
            }
            else
            {
                target.Logos[logo.Key] = logo.Value;
            }
        }
    }

    private static bool HasPrimaryProperties(this Logo logo)
    {
        return logo.Path.Length > 0 || logo.Title.Length > 0;
    }
}
