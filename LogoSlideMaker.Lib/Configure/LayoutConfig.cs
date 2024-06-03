namespace LogoSlideMaker.Configure;

public record Config
{
    /// <summary>
    /// Title of the overall document
    /// </summary>
    public string? Title { get; set; }

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

}
