namespace LogoSlideMaker.Public;

public interface IDefinition
{
    string? Title { get; }
    ICollection<IVariant> Variants { get; }

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    ICollection<string> ImagePaths { get; }

    string? OutputFileName { get; }

    string? TemplateSlidesFileName { get; }

    bool Listing { get; }

    void RenderListing(TextWriter output);
}
