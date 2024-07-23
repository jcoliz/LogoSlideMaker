namespace LogoSlideMaker.Configure;

/// <summary>
/// Allows user to set out/template files using TOML file
/// </summary>
public record FilesConfig
{
    /// <summary>
    /// Powerpoint output
    /// </summary>
    public string? Output { get; set; } = null;

    /// <summary>
    /// Configuration for what the slides should look like before
    /// logos are added
    /// </summary>
    public TemplateConfig Template { get; set; } = new();

    /// <summary>
    /// Configuration for what files should be included into this definition
    /// </summary>
    public IncludeConfig Include { get; set; } = new();
}

public record TemplateConfig
{
    /// <summary>
    /// Powerpoint presentation template
    /// </summary>
    public string? Slides { get; set; }

    public List<string> Bitmaps { get; set; } = new();
}

public record IncludeConfig
{
    /// <summary>
    /// Filename of definition where we should extract logos from
    /// </summary>
    public string? Logos { get; set; }
}