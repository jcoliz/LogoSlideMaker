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

    /// <summary>
    /// Override certain definition values based on user-selected options
    /// </summary>
    void OverrideWithOptions(string? template, bool? listing, string? output);

    void RenderListing(TextWriter output);
}
