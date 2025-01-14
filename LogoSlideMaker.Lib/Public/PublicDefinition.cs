using DocumentFormat.OpenXml.InkML;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
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

    public string? OutputFileName => definition.Files.Output;

    public bool Listing => definition.Render.Listing;

    public void OverrideWithOptions(string? template, bool? listing, string? output)
    {
        if (!string.IsNullOrWhiteSpace(template))
        {
            definition.Files.Template.Slides = template;
        }

        if (listing.HasValue)
        {
            definition.Render.Listing = listing.Value;
        }

        if (!string.IsNullOrWhiteSpace(output))
        {
            definition.Files.Output = output;
        }
    }

    public void RenderListing(TextWriter output)
    {
        var markdown = new List<string>([$"# {definition.Layout.Title}"]);

        markdown.AddRange(definition.Variants.SelectMany(x => new LayoutEngine(definition, x).AsMarkdown()));

        foreach (var line in markdown)
        {
            output.WriteLine(line);
        }
    }
}
