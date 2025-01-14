using DocumentFormat.OpenXml.InkML;
using LogoSlideMaker.Configure;
using System.Collections.Immutable;

namespace LogoSlideMaker.Public;

internal class PublicDefinition(Definition definition) : IDefinition
{
    public string? Title => definition.Layout.Title;

    public ICollection<IVariant> Variants { get; } = definition.Variants.Select(x => new PublicVariant(definition,x)).Cast<IVariant>().ToImmutableList();

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    public ICollection<string> ImagePaths =>
        definition.Logos.Select(x => x.Value.Path).Concat(definition.Files.Template.Bitmaps).ToHashSet();
}
