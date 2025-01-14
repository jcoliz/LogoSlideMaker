namespace LogoSlideMaker.Models;

/// <summary>
/// Defines the size of an on-screen space
/// </summary>
public record Size
{
    /// <summary>
    /// Width of space in inches
    /// </summary>
    public decimal Width { get; set; }

    /// <summary>
    /// Height of space in inches
    /// </summary>
    public decimal? Height { get; set; }
}
