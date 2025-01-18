namespace LogoSlideMaker.Configure;

/// <summary>
/// Configuration which affects rendering
/// </summary>
internal record RenderConfig
{
    /// <summary>
    /// Vertical distance from middle of icon to top of text, in inches
    /// </summary>
    public decimal TextDistace { get; set; } = 0.5m;

    /// <summary>
    /// Default width of text under logos, in inches
    /// </summary>
    public decimal TextWidth { get; set; } = 1m;

    /// <summary>
    /// Height of text box under logos, in inches
    /// </summary>
    public decimal TextHeight { get; set; } = 0.5m;

    /// <summary>
    /// Width &amp; height of square icons, in inches
    /// </summary>

    public decimal IconSize { get; set; } = 0.5m;

    /// <summary>
    /// Dots (pixels) per inch
    /// </summary>
    public decimal Dpi { get; set; } = 96m;

    public decimal FontSize { get; set; } = 12m;

    public string FontName { get; set; } = "sans";

    public string FontColor { get; set; } = "000000";

    /// <remarks>
    /// Unused. This was needed before we had transparency
    /// </remarks>
    public string BackgroundColor { get; set; } = "FFFFFF";

    /// <remarks>
    /// Unused. Bring this back as part of rational styles
    /// </remarks>
    public string FontColorDark { get; set; } = "FFFFFF";

    /// <remarks>
    /// Unused. Dark mode never happened.
    /// </remarks>
    public string BackgroundColorDark { get; set; } = "000000";

    /// <remarks>
    /// Unused. Dark mode never happened.
    /// </remarks>
    public string PaddingColorDark { get; set; } = "FFFFFF";

    /// <summary>
    /// Height of title box over logo boxes, in inches, or null if
    /// titles should not be rendered
    /// </summary>
    public decimal? TitleHeight { get; set; }

    public decimal TitleFontSize { get; set; } = 14;

    public string TitleFontName { get; set; } = "sans";

    public string TitleFontColor { get; set; } = "000000";

    /// <summary>
    /// In dark mode, how much padding to add around all sides
    /// </summary>
    /// <remarks>
    /// Unused
    /// </remarks>
    public double PaddingDark { get; set; }

    /// <summary>
    /// Whether to render in dark mode, else is in light mode
    /// </summary>
    /// <remarks>
    /// Unused. And this is totally the wrong way to go about this
    /// </remarks>
    public bool Dark { get; set; }

    /// <summary>
    /// Whether to also display a text list of apps to the console
    /// </summary>
    /// <remarks>
    /// This can also be overriden on command line with "--list"
    /// </remarks>
    public bool Listing { get; set; }
}
