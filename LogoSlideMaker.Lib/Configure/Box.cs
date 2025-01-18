using LogoSlideMaker.Models;

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

    /// <summary>
    /// Which page is this on?
    /// </summary>
    /// <remarks>
    /// Obsolete. Prefer using `Layout`
    /// 
    /// Think of a 'page' as a logical grouping of boxes. 
    /// 
    /// This box will be included in the layout if the variants include this page, or
    /// if this is zero and the variant specifies no pages, then it will also be
    /// included.
    /// 
    /// Furthermore, this is used for locations. If a location is specified, this
    /// page value is used as a lookup into the locations table to find locatino
    /// information for this page.
    /// </remarks>
    public int Page { get; set; }

    /// <summary>
    /// Logical grouping of boxes
    /// </summary>
    /// <remarks>
    /// Replaces `Page`
    /// </remarks>
    public string? Layout { get; set; }

    /// <summary>
    /// Name of location on this page or layout
    /// </summary>
    /// <remarks>
    /// If set, will pull box size/position from central list of locations. 
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

    /// <summary>
    /// Number of rows we will have
    /// </summary>
    /// <remarks>
    /// Overrides logos.Count, previously used 
    /// </remarks>
    public int? NumRows { get; set; }

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
