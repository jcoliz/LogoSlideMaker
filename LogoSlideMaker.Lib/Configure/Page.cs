namespace LogoSlideMaker.Configure;

/// <summary>
/// A numbered collection of boxes
/// </summary>
/// <remarks>
/// Can be specified for in a Variant
/// </remarks>
public record Page
{
    public int Number { get; set; }
    public List<Box> Box { get; set; } = new();
}
