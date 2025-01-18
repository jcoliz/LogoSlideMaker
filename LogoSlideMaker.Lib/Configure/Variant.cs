namespace LogoSlideMaker.Configure;

/// <summary>
/// Describes a variation of logos. One variant will be rendered on each slide.
/// </summary>
/// <remarks>
/// I've now been referring to `Variant` interchangeabley with `Slide`. In
/// the future, I'll rename this to `Slide`, as it describes a single slide.
/// </remarks>
internal record Variant
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
    /// <remarks>
    /// Obsolete. Use `Background`
    /// 
    /// In practice, this has become confusing with 'page'. 'Page' tells where to get boxes, 
    /// 'Source' tells us what to put them on. Not super clear. 
    /// 
    /// This came about for the light/dark slides. light/dark need the same boxes, but have
    /// a different background.
    /// </remarks>
    public int Source 
    { 
        get => Background - 1;
        set => Background = value + 1;
    }

    /// <summary>
    /// Which background slide # to use as the background, starting from 1, or 0 for no background
    /// </summary>
    public int Background { get; set; } = 1;

    /// <summary>
    /// Which pages of boxes to include
    /// </summary>
    /// <remarks>
    /// If not specificed, will only includes 'loose' boxes, not assigned to any
    /// particular page.
    /// 
    /// I think page is specified backwards. The idea behind having multiple 'pages' here is that we could
    /// duplicate boxes on multiple different slides. We really haven't been using that, although
    /// it might be a good idea to start.
    /// 
    /// Instead of using this facility, I've added the ability to steal logos from the previous
    /// box with the same name. This is a more flexible approach anyway.
    /// </remarks>
    public List<int> Pages { get; set; } = new();

    /// <summary>
    /// What named layout to use on this slide 
    /// </summary>
    /// <remarks>
    /// This replaces the use of numbered pages. If there are no pages and no layout specified,
    /// then we will just get 'loose' boxes
    /// </remarks>
    public string? Layout { get; set; } = null;

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

    /// <summary>
    /// Default text width for this variant
    /// </summary>
    /// <remarks>
    /// Overrides text width for the whole page, but can be overriden by 
    /// logo-specific text width
    /// </remarks>
    public decimal? TextWidth { get; set; }
}

/// <summary>
/// Specification for logo masking
/// </summary>
/// <remarks>
/// Logos with these tags will be replaced by the mask logo
/// </remarks>
internal record Masking 
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
