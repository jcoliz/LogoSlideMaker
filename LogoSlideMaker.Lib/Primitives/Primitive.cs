using LogoSlideMaker.Configure;

namespace LogoSlideMaker.Primitives;

public record Primitive
{
    public Rectangle Rectangle { get; init; } = new();
}

public record TextPrimitive: Primitive
{
    public string Text { get; init; } = string.Empty;
}

public record ImagePrimitive: Primitive
{
    public string Path { get; init; } = string.Empty;
}
