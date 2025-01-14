using DocumentFormat.OpenXml.InkML;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;

namespace LogoSlideMaker.Public;

internal class PublicVariant(Definition definition, Variant variant) : IVariant
{
    public string Name => variant.Name;

    public ICollection<string> Description => variant.Description;

    /// <summary>
    /// Generate all primitives needed to display this slide
    /// </summary>
    public ICollection<Primitive> GeneratePrimitives(IGetImageAspectRatio bitmaps)
    {
        var result = new List<Primitive>();

        // Layout slide
        var layout = PopulateLayout();

        //
        // Add background primitives
        //

        var bgRect = new Rectangle() { X = 0, Y = 0, Width = 1280 /*PlatenSize.Width*/, Height = 720 /*PlatenSize.Height*/ };

        // If there is a bitmap template, draw that
        var definedBitmaps = definition.Files.Template.Bitmaps;
        var sourceBitmap = layout.Variant.Source;
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
        var generator = new PrimitivesEngine(definition.Render, bitmaps);
        result.AddRange(layout.Logos.SelectMany(generator.ToPrimitives));

        // Add box title primitives
        result.AddRange(layout.Text.SelectMany(generator.ToPrimitives));

        //
        // Add extents primitives
        //

        // TODO: Need to reduce to only on this slide!!
        result.AddRange(
            definition.Boxes
                .Where(x => x.Outer is not null)
                .Select(x => new RectanglePrimitive()
                {
                    Rectangle = x.Outer! with
                    {
                        X = x.Outer.X * 96m,
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
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();
        return layout;
    }
}
