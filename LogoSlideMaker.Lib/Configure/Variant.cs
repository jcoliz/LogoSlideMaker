namespace LogoSlideMaker.Configure;

/// <summary>
/// Describes a variation of logos. One variant will be rendered on each slide.
/// </summary>
/// <remarks>
/// I've now been referring to `Variant` interchangeabley with `Slide`. In
/// the future, I'll rename this to `Slide`, as it describes a single slide.
/// </remarks>
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

    /// <summary>
    /// Specification for logo masking
    /// </summary>
    public Masking Mask { get; set; } = new();

    /// <summary>
    /// What alternate language should be used for this variant
    /// </summary>
    /// <remarks>
    /// Will display the text in lang.xxx logos, boxes, and slide title
    /// NOTE: Not implemented yet
    /// </remarks>
    public string? Lang { get; set; }
}

/// <summary>
/// Specification for logo masking
/// </summary>
/// <remarks>
/// Logos with these tags will be replaced by the mask logo
/// </remarks>
public record Masking 
{
    /// <summary>
    /// Which logo to use as the mask
    /// </summary>
    public string Logo { get; set; } = string.Empty;

    /// <summary>
    /// Which tags identify logos which should should be masked
    /// </summary>
    public List<string> Tags { get; set; } = new();
}
