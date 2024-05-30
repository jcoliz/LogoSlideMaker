using ShapeCrawler;
using System.Drawing.Imaging;

/// <summary>
/// The overall run definition as loaded in as the top-level toml file
/// </summary>
public record Definition
{
    /// <summary>
    /// Global setup configuration
    /// </summary>
    public Config Layout { get; set; } = new();

    public RenderConfig Render { get; set; } = new();

    /// <summary>
    /// Variations of the logos which each get their own slide
    /// </summary>
    public List<Variant> Variants { get; set; } = new();

    /// <summary>
    /// The actual logos to be displayed, indexed by id
    /// </summary>
    public Dictionary<string,Logo> Logos { get; set; } = [];

    /// <summary>
    /// Rows of logo positions describing how the logos are laid out
    /// </summary>
    /// <remarks>
    /// DEPRECATED, use Boxes
    /// </remarks>
    public List<Row> Rows { get; set; } = new();

    /// <summary>
    /// Multi-line boxes of logo positions describing how the logos are laid out
    /// </summary>
    public List<Box> Boxes { get; set; } = new();
}

/// <summary>
/// Describes a variation of logos. One variant will be rendered on each slide.
/// </summary>
public record Variant
{
    /// <summary>
    /// Human-readable name
    /// </summary>
    /// <remarks>
    /// Ideally, would be added to slide notes
    /// </remarks>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description
    /// </summary>
    /// <remarks>
    /// Ideally, would be added to slide notes
    /// </remarks>
    public List<string> Description { get; set; } = new();

    /// <summary>
    /// Which tags identify logos which should hold a blank space instead of
    /// showing the logo
    /// </summary>
    public List<string> Blank { get; set; } = new();

    /// <summary>
    /// Which tags identify logos which should should be included
    /// </summary>
    /// <remarks>
    /// By default, any logos with tags are excluded from each variant
    /// </remarks>
    public List<string> Include { get; set; } = new();

    /// <summary>
    /// Which slide # to use as the basis, starting from 0
    /// </summary>
    public int Source { get; set; }
}

/// <summary>
/// Details about a particular logo glyph
/// </summary>
public record Logo
{
    /// <summary>
    /// Human-readble title to show under the logo
    /// </summary>
    public string Title { get; set; } = string.Empty;

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

/// <summary>
/// A method to specify multiple rows in one declaration
/// </summary>
public record Box
{
    public decimal XPosition { get; set; }
    public decimal? YPosition { get; set; } // Calculate based on BoxSpacing if missing
    public decimal? Width { get; set; }
    public int MinColumns { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<int,List<string>> Logos { get; set; } = new Dictionary<int,List<string>>();
}

public record Row
{
    public decimal XPosition { get; set; }
    public decimal YPosition { get; set; }
    public decimal Width { get; set; }
    public int MinColumns { get; set; }
    public List<string> Logos { get; set; } = new List<string>();
    public int NumItems => Math.Max( Logos.Count, MinColumns );
    public decimal Spacing => (NumItems > 1) ? Width / (NumItems - 1) : 0;
}

public enum Commands { Invalid = 0, End = 1 }

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

