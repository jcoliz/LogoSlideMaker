namespace LogoSlideMaker.Configure;

/// <summary>
/// A collection of rows specified in a single declaration
/// </summary>
public record Box
{
    // TODO refactor to a rectqngle
    public decimal? XPosition { get; set; }
    public decimal? YPosition { get; set; }
    public decimal? Width { get; set; }

    /// <summary>
    /// Dimensions of the outer container box
    /// </summary>
    /// <remarks>
    /// Can be used instead of explicit dimensions, will use configured padding to set
    /// those values
    /// </remarks>
    public Rectangle? Outer { get; set; }

    /// <summary>
    /// Additional Padding on X axis. In addition to definition-wide default padding
    /// </summary>
    public decimal? MorePaddingX { get; set; }

    public int MinColumns { get; set; }
    public int Page { get; set; }

    /// <summary>
    /// Whether row should be re-composed to flow nicely
    /// </summary>
    public bool AutoFlow { get; set; }
    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Localized titles
    /// </summary>
    public Dictionary<string,string> Lang { get; set; } = new();

    public Dictionary<int,List<string>> Logos { get; set; } = new Dictionary<int,List<string>>();
}

public record Size
{
    public decimal Width { get; set; }
    public decimal? Height { get; set; }
}

public record Rectangle: Size
{
    public decimal X { get; set; }
    public decimal? Y { get; set; }
}

public record Edge
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Length { get; set; }
    public EdgeKind Kind { get; set; }
}

// Feel free to define more kinds as more are used!
public enum EdgeKind
{
    Undefined = 0,
    Bottom = 1
}

public enum TextSyle
{
    Invisible = 0,
    Logo = 1,
    BoxTitle = 2
}