namespace LogoSlideMaker.Configure;

/// <summary>
/// Details about a particular logo glyph
/// </summary>
/// <remarks>
/// Specify each logo once. Refer to it in as many rows, boxes, and/or
/// variants as needed.
/// </remarks>
internal record Logo
{
    /// <summary>
    /// Human-readble title to show under the logo
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Localized titles
    /// </summary>
    public Dictionary<string,string> Lang { get; set; } = new();

    /// <summary>
    /// Where to find the image data
    /// </summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>
    /// Where to find the image data when displaying on a dark background
    /// </summary>
    public string? PathDark { get; set; }

    /// <summary>
    /// Tags which describe the logo, used by variants to pick which logos
    /// go in which variant
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Override default text width for this logo
    /// </summary>
    public decimal? TextWidth { get; set; }

    /// <summary>
    /// Override default scale for this logo
    /// </summary>
    public decimal Scale { get; set; } = 1.0m;

    /// <summary>
    /// True if this logo only looks good against a light background
    /// </summary>
    /// <remarks>
    /// Ideally it would be replaces, or at least filled with white when
    /// presented on a dark background.
    /// </remarks>
    public bool Light { get; set; } = false;

    /// <summary>
    /// Alt-text to use for the logo
    /// </summary>
    /// <remarks>
    /// Only used if text in the logo is required for understanding
    /// </remarks>
    public string? AltText { get; set; }
}
