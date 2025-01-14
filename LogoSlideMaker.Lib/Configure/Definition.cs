using LogoSlideMaker.Models;

namespace LogoSlideMaker.Configure;

/// <summary>
/// The overall run definition as loaded in as the top-level toml file
/// </summary>
internal record Definition
{
    public FilesConfig Files { get; set; } = new();

    /// <summary>
    /// Global setup configuration
    /// </summary>
    public Config Layout { get; set; } = new();

    public RenderConfig Render { get; set; } = new();

    /// <summary>
    /// Variations of the logos which each get their own slide
    /// </summary>
    public List<Variant> Variants { get; set; } = new();

    public Dictionary<int,Dictionary<string,Location>> Locations { get; set; } = new();

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

    /// <summary>
    /// After loading a definition, call this to complete any post-load processing
    /// </summary>
    public void ProcessAfterLoading()
    {
        foreach (var box in Boxes)
        {
            if (Locations.Count > 0)
            {
                box.SetLocationProperties(Locations);
            }

            // If a box has no logos, grab them from first box with same title
            if (box.Logos.Keys.Count == 0)
            {
                var found = Boxes.First(x=>x.Title == box.Title);
                if (found.Logos.Keys.Count > 0)
                {
                    box.Logos = found.Logos;                    
                }
            }
        }
    }
}

internal record Location: Rectangle
{
    /// <summary>
    /// Number of rows this location can handle
    /// </summary>
    public int? NumRows { get; set; }
}
