using LogoSlideMaker.Configure;
using System.Collections.Immutable;

namespace LogoSlideMaker.Public;

internal class PublicDefinition(Definition definition) : IDefinition
{
    public string? Title => definition.Layout.Title;

    public ICollection<IVariant> Variants { get; } = definition.Variants.Select(x => new PublicVariant(x)).Cast<IVariant>().ToImmutableList();
}
