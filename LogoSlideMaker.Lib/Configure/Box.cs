namespace LogoSlideMaker.Configure;

/// <summary>
/// A collection of rows specified in a single declaration
/// </summary>
internal record Box
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
    /// Name of location on this page
    /// </summary>
    /// <remarks>
    /// If set, will pull box size/position from central list of locations
    /// </remarks>
    public string? Location { get; set; }

    /// <summary>
    /// Whether row should be re-composed to flow nicely
    /// </summary>
    public bool AutoFlow { get; set; } = true;

    public string Title { get; set; } = string.Empty;
    /// <summary>
    /// Localized titles
    /// </summary>
    public Dictionary<string,string> Lang { get; set; } = new();

    public Dictionary<int,List<string>> Logos { get; set; } = new Dictionary<int,List<string>>();

    public void SetLocationProperties(Dictionary<int, Dictionary<string, Location>> locations)
    {
        if (!string.IsNullOrEmpty(Location))
        {
            Outer = locations[this.Page][this.Location];
            NumRows = locations[this.Page][this.Location].NumRows;
        }
    }

    /// <summary>
    /// Number of rows we will have
    /// </summary>
    /// <remarks>
    /// Overrides logos.Count, previously used 
    /// </remarks>
    public int? NumRows { get; set; }

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

internal record Edge
{
    public decimal X { get; set; }
    public decimal Y { get; set; }
    public decimal Length { get; set; }
    public EdgeKind Kind { get; set; }
}

// Feel free to define more kinds as more are used!
internal enum EdgeKind
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