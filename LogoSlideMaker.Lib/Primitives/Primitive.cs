using LogoSlideMaker.Models;

namespace LogoSlideMaker.Primitives;

/// <summary>
/// Conceptually what does this primitive represent
/// </summary>
public enum PrimitivePurpose { Base = 0, Background, Extents }

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
    public string Text { get; init; } = string.Empty;

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
