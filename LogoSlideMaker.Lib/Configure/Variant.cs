namespace LogoSlideMaker.Configure;

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

    /// <summary>
    /// Which pages of boxes to include
    /// </summary>
    /// <remarks>
    /// By default, only includes 'loose' boxes, not assigned to any
    /// particular page.
    /// </remarks>
    public List<int> Pages { get; set; } = new();

    public Masking Mask { get; set; } = new();
}

public record Masking 
{
    public string Logo { get; set; } = string.Empty;

    public List<string> Tags { get; set; } = new();
}
