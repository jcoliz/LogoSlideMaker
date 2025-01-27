using LogoSlideMaker.Models;

namespace LogoSlideMaker.Primitives;

/// <summary>
/// Conceptually what does this primitive represent
/// </summary>
public enum PrimitivePurpose 
{
    /// <summary>
    /// Primitive is fundamental to all visualizations and should always be included
    /// </summary>
    Base = 0, 

    /// <summary>
    /// Primitive forms the background, so would not be included if background is coming from slide template
    /// </summary>
    Background, 

    /// <summary>
    /// Primitive displays the extents of bounding regions, which user may decide to show or hide
    /// </summary>
    Extents 
}

/// <summary>
/// Base drawing primitive
/// </summary>
public record Primitive
{
    /// <summary>
    /// Where this should be drawn
    /// </summary>
    public Rectangle Rectangle { get; init; } = new();

    /// <summary>
    /// Conceptually what does this primitive represent
    /// </summary>
    public PrimitivePurpose Purpose { get; init; } = PrimitivePurpose.Base;
}

/// <summary>
/// Represents text to be drawn
/// </summary>
public record TextPrimitive: Primitive
{
    /// <summary>
    /// The text to be drawn
    /// </summary>
    public string Text { get; init; } = string.Empty;

    /// <summary>
    /// The style in which to draw the text
    /// </summary>
    public TextSyle Style { get; init; } = TextSyle.Invisible;
}

/// <summary>
/// Represents image to be drawn
/// </summary>
public record ImagePrimitive: Primitive
{
    /// <summary>
    /// Path to on-disk location of image file
    /// </summary>
    public string Path { get; init; } = string.Empty;

    /// <summary>
    /// How to crop this image
    /// </summary>
    public Frame? Crop { get; init; }
}

/// <summary>
/// Represents a simple rectangle to be drawn
/// </summary>
public record RectanglePrimitive : Primitive
{
    /// <summary>
    /// Whether the space should be filled
    /// </summary>
    /// <remarks>
    /// TODO: Is this even used??
    /// </remarks>
    public bool Fill { get; init; } = false;
}

/// <summary>
/// A frame of insets around edges of a rectangle
/// </summary>
/// <remarks>
/// For starters, just supporting right side
/// </remarks>
public record Frame
{
    /// <summary>
    /// What amount (0.0-1.0) of the image to remove from the right edge
    /// </summary>
    public decimal Right { get; set; }
}
