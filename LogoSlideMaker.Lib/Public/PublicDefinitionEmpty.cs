
namespace LogoSlideMaker.Public;

internal class PublicDefinitionEmpty : IDefinition
{
    public string? Title => "Untitled";

    public IList<IVariant> Variants => new List<IVariant>() { new PublicVariantEmpty(this,0) };

    public ICollection<string> ImagePaths => [];

    public string? OutputFileName => null;

    public string? TemplateSlidesFileName => null;

    public bool Listing => false;

    public void RenderListing(TextWriter output)
    {
    }
}