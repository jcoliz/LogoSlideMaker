using LogoSlideMaker.Configure;

namespace LogoSlideMaker.Layout;

/// <summary>
/// Decompose a single logo entry within a row into component parts
/// </summary>
/// <remarks>
/// The `logos=` line in a row can encode a lot of information. This breaks
/// it apart into well-defined symbols.
/// </remarks>
public record Entry
{
    /// <summary>
    /// Construct from a row logo entry
    /// </summary>
    /// <param name="value">Logo as specified in the original box line</param>
    public Entry(string value)
    {
        var split = value.Split(':');
        var logoId = split[0];

        if (logoId.StartsWith('@'))
        {
            Command = Enum.Parse<Commands>(logoId[1..], ignoreCase:true);
        }
        else
        {
            Id = logoId;
        }

        var tags = split.Skip(1).GroupBy(x=>!x.StartsWith('!')).ToDictionary(x=>x.Key,x=>x);
        Tags = tags.GetValueOrDefault(true)?.ToArray() ?? [];
        NotTags = tags.GetValueOrDefault(false)?.Select(x=>x[1..]).ToArray() ?? [];
    }

    /// <summary>
    /// Logo ID, or null if is not a logo
    /// </summary>
    /// <remarks>
    /// Used to lookup into Definition.Logos
    /// </remarks>
    public string? Id { get; }

    /// <summary>
    /// Processing command
    /// </summary>
    /// <remarks>
    /// Set by "@{command}" in row logos
    /// </remarks>
    public Commands? Command { get; }

    /// <summary>
    /// Entry-specific tags, will include this entry if match variant
    /// </summary>
    /// <remarks>
    /// Set with "app:tag" in row logos
    /// </remarks>
    public string[] Tags { get; }

    /// <summary>
    /// Entry-specific disincluding tags, will include this entry if does NOT match variant
    /// </summary>
    /// <remarks>
    /// Set with "app:!tag" in row logos
    /// </remarks>
    public string[] NotTags { get; }
}

