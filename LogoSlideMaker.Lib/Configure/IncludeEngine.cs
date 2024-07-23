using DocumentFormat.OpenXml.Linq;
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
        // All the logo keys where we already define primary properties in the target, so
        // we will want to IGNORE them from the source
        var primary = target.Logos.Where(x => x.Value.HasPrimaryProperties()).Select(x => x.Key).ToHashSet();

        // All the logos which specify overriding properties, focused down to just the overriding properies themselves
        // As we add more overridable properties, we'd update this.
        var overrides = target.Logos.Where(x => x.Value.HasOverridableProperties())
            .ToDictionary(x => x.Key, y => new Logo() { TextWidth = y.Value.TextWidth, Scale = y.Value.Scale });

        // Extract the logos from the source, excluding existing logos, and stripping out the text width
        var logos = source.Logos.Select(x => (x.Key, Value: x.Value with { TextWidth = null })).Where(x => !primary.Contains(x.Key));

        // Merge those logos in
        foreach(var (Key, Value) in logos)
        {
            if (overrides.TryGetValue(Key, out var o))
            {
                // TODO: Consider whether these overrides should be applied separately
                target.Logos[Key] = Value with { TextWidth = o.TextWidth, Scale = o.Scale };
            }
            else
            {
                target.Logos[Key] = Value;
            }
        }
    }

    private static bool HasPrimaryProperties(this Logo logo)
    {
        return logo.Path.Length > 0 || logo.Title.Length > 0;
    }

    private static bool HasOverridableProperties(this Logo logo)
    {
        // TODO: Scale should be nullable. Here it DOES matter whether or not scale was
        // specified

        return logo.TextWidth.HasValue || logo.Scale != default;
    }
}
