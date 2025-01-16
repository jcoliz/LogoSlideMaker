using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Models;
using LogoSlideMaker.Primitives;

namespace LogoSlideMaker.Public;

internal class PublicVariant(PublicDefinition definition, Variant variant, int index) : IVariant
{
    public string Name => variant.Name;

    public ICollection<string> Description => variant.Description;

    public ICollection<string> Notes
    {
        get
        {
            _layout ??= PopulateLayout();
            return [$"Logo count: {_layout.Logos.Count(y => y.Logo != null)}"];
        }
    }

    public int Source => variant.Source;

    public IReadOnlyDictionary<TextSyle, ITextStyle> TextStyles => definition.TextStyles;

    public int Index => index;

    public string? DocumentTitle => 
        variant.Lang is not null && definition.Definition.Layout.Lang.TryGetValue(variant.Lang, out var loctitle) 
        ? loctitle 
        : definition.Definition.Layout.Title;

    private SlideLayout? _layout;

    /// <summary>
    /// Generate all primitives needed to display this slide
    /// </summary>
    public ICollection<Primitive> GeneratePrimitives(IGetImageAspectRatio bitmaps)
    {
        var result = new List<Primitive>();

        // Layout slide
        _layout = PopulateLayout();

        //
        // Add background primitives
        //

        var bgRect = new Rectangle() { X = 0, Y = 0, Width = 1280 /*PlatenSize.Width*/, Height = 720 /*PlatenSize.Height*/ };

        // If there is a bitmap template, draw that
        var definedBitmaps = definition.Definition.Files.Template.Bitmaps;
        var sourceBitmap = _layout.Variant.Source;
        if (definedBitmaps is not null && definedBitmaps.Count > sourceBitmap && bitmaps.Contains(definedBitmaps[sourceBitmap]))
        {
            result.Add(new ImagePrimitive()
            {
                Rectangle = bgRect,
                Path = definedBitmaps[sourceBitmap],
                Purpose = PrimitivePurpose.Background
            });
        }
        else
        {
            // Else Draw a white background
            result.Add(new RectanglePrimitive()
            {
                Rectangle = bgRect,
                Fill = true,
                Purpose = PrimitivePurpose.Background
            });
        }

        //
        // Add base primitives
        //

        // Add needed primitives for each logo
        var generator = new PrimitivesEngine(definition.Definition.Render, bitmaps);
        result.AddRange(_layout.Logos.SelectMany(generator.ToPrimitives));

        // Add box title primitives
        result.AddRange(_layout.Text.SelectMany(generator.ToPrimitives));

        //
        // Add extents primitives
        //

        // TODO: Need to reduce to only on this slide!!
        result.AddRange(
            definition.Definition.Boxes
                .Where(x => x.Outer is not null)
                .Select(x => new RectanglePrimitive()
                {
                    Rectangle = new Rectangle()
                    {
                        X = x.Outer!.X * 96m, // TODO: Respect definition DPI!!
                        Y = x.Outer.Y * 96m,
                        Width = x.Outer.Width * 96m,
                        Height = x.Outer.Height * 96m
                    },
                    Purpose = PrimitivePurpose.Extents
                }
            )
        );

        return result;
    }

    /// <summary>
    /// Create a slide layout for the this variant
    /// </summary>
    private SlideLayout PopulateLayout()
    {
        var engine = new LayoutEngine(definition.Definition, variant);
        var layout = engine.CreateSlideLayout();
        return layout;
    }
}

internal class PublicVariantEmpty(IDefinition definition, int index) : IVariant
{
    public string Name => string.Empty;

    public ICollection<string> Description => [];

    public ICollection<string> Notes => [];

    public int Source => 0;

    public IReadOnlyDictionary<TextSyle, ITextStyle> TextStyles { get; } = new Dictionary<TextSyle, ITextStyle>();

    public int Index => index;

    public IVariant Next
    {
        get
        {
            if (index + 1 >= definition.Variants.Count)
            {
                return definition.Variants[0];
            }
            else
            {
                return definition.Variants[index + 1];
            }
        }
    }

    public IVariant Previous
    {
        get
        {
            if (index == 0)
            {
                return definition.Variants[^1];
            }
            else
            {
                return definition.Variants[index - 1];
            }
        }
    }

    public string? DocumentTitle => "Untitled";

    public ICollection<Primitive> GeneratePrimitives(IGetImageAspectRatio bitmaps) => [];
}