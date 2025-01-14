using LogoSlideMaker.Configure;

namespace LogoSlideMaker.Primitives;

public enum PrimitivePurpose { Base = 0, Background, Extents }

public record Primitive
{
    public Rectangle Rectangle { get; init; } = new();

    public PrimitivePurpose Purpose { get; init; } = PrimitivePurpose.Base;
}

public record TextPrimitive: Primitive
{
    public string Text { get; init; } = string.Empty;

    public TextSyle Style { get; init; } = TextSyle.Invisible;
}

public record ImagePrimitive: Primitive
{
    public string Path { get; init; } = string.Empty;
}

public record RectanglePrimitive : Primitive
{
    public bool Fill { get; init; } = false;
}
