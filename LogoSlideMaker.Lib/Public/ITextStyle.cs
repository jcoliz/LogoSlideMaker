namespace LogoSlideMaker.Public;

/// <summary>
/// Defines the appearance of a certain kind of text
/// </summary>
public interface ITextStyle
{
    /// <summary>
    /// Size in points
    /// </summary>
    decimal FontSize { get; }

    /// <summary>
    /// Latin name of font (Should be known to PowerPoint)
    /// </summary>
    string FontName { get; }

    /// <summary>
    /// Color of font. In 6-digit hex color
    /// </summary>
    string FontColor { get; }
}
