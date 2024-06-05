namespace LogoSlideMaker.Configure;

public record Config
{
    /// <summary>
    /// Title of the overall document
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// Localized titles
    /// </summary>
    public Dictionary<string,string> Lang { get; set; } = new();

    /// <summary>
    /// Default vertical space between successive lines, in inches
    /// </summary>
    /// <remarks>
    /// Only used when lines are being automatically spaced
    /// </remarks>
    public decimal LineSpacing { get; set; }

    /// <summary>
    /// Default vertical space between successive boxes, in inches
    /// </summary>
    /// <remarks>
    /// Only used when boxes are being automatically spaced
    /// </remarks>
    public decimal BoxSpacing { get; set; }

    /// <summary>
    /// Default width of row, in inches
    /// </summary>
    public decimal? DefaultWidth { get; set; }

    /// <summary>
    /// Distance from a box's outer container to outside of square icon and
    /// outside of default-size text block.
    /// </summary>
    /// <remarks>
    /// Only used when boxes have an outer container specified
    /// </remarks>
    public decimal? Padding { get; set; }
}
