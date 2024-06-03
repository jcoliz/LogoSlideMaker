namespace LogoSlideMaker.Configure;

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
