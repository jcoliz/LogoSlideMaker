namespace LogoSlideMaker.Models;

/// <summary>
/// The different kinds of text which we can render
/// </summary>
/// <remarks>
/// There should be an available style definition for all but Invisible text
/// </remarks>
public enum TextSyle
{
    /// <summary>
    /// The text is not to be drawn
    /// </summary>
    Invisible = 0,

    /// <summary>
    /// Text annotating a logo
    /// </summary>
    Logo = 1,

    /// <summary>
    /// Text annotating a group of logos
    /// </summary>
    BoxTitle = 2
}