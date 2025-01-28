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
    /// <remarks>
    /// If null, then we just want to put a placeholder here for now
    /// </remarks>
    public string? Path { get; set; }

    /// <summary>
    /// Where to find the image data when displaying on a dark background
    /// </summary>
    /// <remarks>
    /// Unused
    /// </remarks>
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
    /// Unused
    ///
    /// Ideally it would be replaces, or at least filled with white when
    /// presented on a dark background.
    /// </remarks>
    public bool Light { get; set; } = false;

    /// <summary>
    /// Alt-text to use for the logo
    /// </summary>
    /// <remarks>
    /// Only used if text in the logo is required for understanding, and is not
    /// repeated in the logo tite
    /// </remarks>
    public string? AltText { get; set; }

    /// <summary>
    /// Cropping frame for image
    /// </summary>
    /// <remarks>
    /// Specifyins what amount of the image should be removed (0.0-1.0) on each
    /// edge
    /// </remarks>
    public Primitives.Frame? Crop { get; set; }

    /// <summary>
    /// Corner radius for logo image
    /// </summary>
    /// <remarks>
    /// Specified in portion of the standard logo width. This allows corner to
    /// scale with larger logos.
    /// 
    /// e.g. if IconSize is 0.5 inches, and CornerRadius is 0.1, then the corners
    /// will be 0.05 inches in radius.
    /// </remarks>
    public decimal? CornerRadius { get; set; }
}
