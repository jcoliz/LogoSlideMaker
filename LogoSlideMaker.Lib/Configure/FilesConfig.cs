namespace LogoSlideMaker.Lib.Configure;

/// <summary>
/// Allows user to set out/template files using TOML file
/// </summary>
public record FilesConfig
{
    public TemplateConfig Template { get; set; } = new();
}

public record TemplateConfig
{
    /// <summary>
    /// Powerpoint presentation template
    /// </summary>
    public string? Slides { get; set; }

    public List<string> Bitmaps { get; set; } = new();
}