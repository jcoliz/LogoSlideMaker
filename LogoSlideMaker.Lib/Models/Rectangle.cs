namespace LogoSlideMaker.Models;

/// <summary>
/// Defines the position and size of an on-screen space
/// </summary>
public record Rectangle : Size
{
    /// <summary>
    /// Distance from left edge of screen to left edge of space in inches
    /// </summary>
    public decimal X { get; set; }

    /// <summary>
    /// Distance from top edge of screen to top edge of space in inches
    /// </summary>
    public decimal? Y { get; set; }
}
