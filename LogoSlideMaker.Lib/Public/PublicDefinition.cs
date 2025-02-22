﻿using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Models;
using System.Collections.Immutable;

namespace LogoSlideMaker.Public;

/// <summary>
/// Implements the public IDefinition interface.
/// </summary>
/// <remarks>
/// Marshalls the internal data into the expected public form
/// </remarks>
internal class PublicDefinition : IDefinition
{
    private readonly Definition definition;
    public PublicDefinition(Definition _definition)
    {
        definition = _definition;

        ProcessAfterLoading();

        if (definition.Variants.Count == 0)
        {
            definition.Variants = [new Variant()];
        }

        Variants = definition.Variants.Select((x,i) => new PublicVariant(this,x,i)).Cast<IVariant>().ToImmutableList();

        // Note that we still use built-in default styles by default.
        // These can be overridden by specifying explicit styles
        var textStyles = new Dictionary<TextSyle, ITextStyle>()
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

        foreach(var definedStyle in definition.TextStyles)
        {
            if (Enum.TryParse<TextSyle>(definedStyle.Key, out var key))
            {
                textStyles[key] = new PublicTextStyle(
                    definedStyle.Value.Size,
                    definedStyle.Value.Name,
                    definedStyle.Value.Color
                );
            }
        }

        TextStyles = textStyles;
    }

    public IList<IVariant> Variants {
        get; private init;
     }

    /// <summary>
    /// All the image paths we would need to render
    /// </summary>
    public ICollection<string> ImagePaths =>
        definition.Logos.Where(x=>!string.IsNullOrWhiteSpace(x.Value.Path)).Select(x => x.Value.Path!).Concat(definition.Files.Template.Bitmaps).ToHashSet();

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

    /// <summary>
    /// After loading a definition, call this to complete any post-load processing
    /// </summary>
    private void ProcessAfterLoading()
    {
        foreach (var box in definition.Boxes)
        {
            if (definition.Locations.Count > 0)
            {
                if (!string.IsNullOrEmpty(box.Location))
                {
                    box.Outer = definition.Locations[box.Page][box.Location];
                    box.NumRows = definition.Locations[box.Page][box.Location].NumRows;
                }
            }

            if (definition.Layouts.Count > 0)
            {
                if (!string.IsNullOrEmpty(box.Layout) && !string.IsNullOrEmpty(box.Location))
                {
                    box.Outer = definition.Layouts[box.Layout][box.Location];
                    box.NumRows = definition.Layouts[box.Layout][box.Location].NumRows;
                }
            }

            // If a box has no logos, grab them from first box with same title
            if (box.Logos.Keys.Count == 0)
            {
                var found = definition.Boxes.First(x=>x.Title == box.Title);
                if (found.Logos.Keys.Count > 0)
                {
                    box.Logos = found.Logos;                    
                }
            }
        }
    }    
}
