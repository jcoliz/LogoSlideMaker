namespace LogoSlideMaker.Configure;

/// <summary>
/// Configuration which affects rendering
/// </summary>
public record RenderConfig
{
    /// <summary>
    /// Vertical distance from middle of icon to top of text, in inches
    /// </summary>
    public decimal TextDistace { get; set; }

    /// <summary>
    /// Default width of text under logos, in inches
    /// </summary>
    public decimal TextWidth { get; set; }

    /// <summary>
    /// Height of text box under logos, in inches
    /// </summary>
    public decimal TextHeight { get; set; }

    /// <summary>
    /// Width & height of square icons, in inches
    /// </summary>

    public decimal IconSize { get; set; }
        /// <summary>
    /// Dots (pixels) per inch
    /// </summary>
    public decimal Dpi { get; set; }

    public int FontSize { get; set; } = 24;

    public string FontName { get; set; } = "sans";

    public string FontColor { get; set; } = "000000";

    public string BackgroundColor { get; set; } = "FFFFFF";

    public string FontColorDark { get; set; } = "FFFFFF";

    public string BackgroundColorDark { get; set; } = "000000";

    public string PaddingColorDark { get; set; } = "FFFFFF";

    /// <summary>
    /// In dark mode, how much padding to add around all sides
    /// </summary>
    public double PaddingDark { get; set; }

    /// <summary>
    /// Whether to render in dark mode, else is in light mode
    /// </summary>
    public bool Dark { get; set; }

    /// <summary>
    /// Whether to also display a text list of apps to the console
    /// </summary>
    /// <remarks>
    /// This can also be overriden on command line with "--list"
    /// </remarks>
    public bool Listing { get; set; }
}