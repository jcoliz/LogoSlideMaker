namespace LogoSlideMaker.Configure;

/// <summary>
/// A collection of rows specified in a single declaration
/// </summary>
public record Box
{
    public decimal XPosition { get; set; }
    public decimal? YPosition { get; set; } // Calculate based on BoxSpacing if missing
    public decimal? Width { get; set; }
    public int MinColumns { get; set; }
    public int Page { get; set; }
    public string Title { get; set; } = string.Empty;
    public Dictionary<int,List<string>> Logos { get; set; } = new Dictionary<int,List<string>>();
}
