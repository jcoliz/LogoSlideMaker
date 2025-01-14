﻿using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Models;
using System.Collections.Immutable;

namespace LogoSlideMaker.Public;

internal class PublicDefinition : IDefinition
{
    private readonly Definition definition;
    public PublicDefinition(Definition _definition)
    {
        definition = _definition;

        definition.ProcessAfterLoading();

        if (definition.Variants.Count == 0)
        {
            definition.Variants = [new Variant()];
        }

        Variants = definition.Variants.Select((x,i) => new PublicVariant(this,x,i)).Cast<IVariant>().ToImmutableList();

        TextStyles = new Dictionary<TextSyle, ITextStyle>()
        {
            { TextSyle.Invisible, new PublicTextStyle() },
            { TextSyle.Logo, new PublicTextStyle(
                definition.Render.FontSize,
                definition.Render.FontName,
                definition.Render.FontColor
            ) },
            { TextSyle.BoxTitle, new PublicTextStyle(
                definition.Render.TitleFontSize,
                definition.Render.TitleFontName,
                definition.Render.TitleFontColor
            ) },
        };
    }

    public string? Title => definition.Layout.Title;

    public IList<IVariant> Variants {
        get; private init;
     }

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    public ICollection<string> ImagePaths =>
        definition.Logos.Select(x => x.Value.Path).Concat(definition.Files.Template.Bitmaps).ToHashSet();

    public string? OutputFileName => definition.Files.Output;

    public string? TemplateSlidesFileName => definition.Files.Template.Slides;

    public bool Listing => definition.Render.Listing;

    internal IReadOnlyDictionary<TextSyle, ITextStyle> TextStyles {
        get; private init;
    }

    internal Definition Definition => definition;

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
