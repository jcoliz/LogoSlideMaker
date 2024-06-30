using ShapeCrawler;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;

namespace LogoSlideMaker.Export;

/// <summary>
/// Render to a presentation slide
/// </summary>
/// <remarks>
/// This is the "export" renderer, which renders to a presentation.
/// Not to be confused with the "preview" renderer, which renders to a Win2d canvas 
/// so user can preview before exporting.
/// </remarks>
public class ExportRenderEngine(RenderConfig config, ImageCache imageCache)
{
    public void Render(Presentation pres, SlideLayout layout, string? dataVersion, IEnumerable<Primitive> primitives)
    {
        var copyingSlide = pres.Slides[layout.Variant.Source];
        pres.Slides.Add(copyingSlide);
        var slide = pres.Slides[^1];

        // TODO: Does this belong here? Or should be it done higher up?
        List<string> notes = [$"Updated: {DateTime.Now:M/dd/yyyy h:mm tt K}"];
        if (dataVersion is not null)
        {
            notes.Add($"Version: {dataVersion}");
        }
        notes.Add($"Logo count: {layout.Logos.Count(y => y.Logo != null)}");
        slide.AddNotes(notes);

        foreach (var p in primitives)
        {
            Draw(p, slide.Shapes);
        }
    }

    private void Draw(Primitive primitive, ISlideShapes target)
    {
        switch (primitive)
        {
            case TextPrimitive text:
                Draw(text, target);
                break;

            case ImagePrimitive image:
                Draw(image, target);
                break;

            case RectanglePrimitive rect:
                Draw(rect, target);
                break;

            default:
                throw new NotImplementedException();
        }
    }
    private void Draw(TextPrimitive primitive, ISlideShapes target)
    {
        target.AddRectangle(100, 100, 100, 100);
        var shape = target[^1];

        shape.X = primitive.Rectangle.X;
        shape.Y = primitive.Rectangle.Y ?? 0;
        shape.Width = primitive.Rectangle.Width;
        shape.Height = primitive.Rectangle.Height ?? primitive.Rectangle.Width;

        var tf = shape.TextFrame;
        tf.Text = primitive.Text;
        tf.LeftMargin = 0;
        tf.RightMargin = 0;
        var font = tf.Paragraphs[0].Portions[0].Font;

        font.Size = config.FontSize;
        font.LatinName = config.FontName;
        font.Color.Update(config.FontColor);
        shape.Fill.SetNoFill();
        shape.Outline.SetNoOutline();
    }

    private void Draw(ImagePrimitive primitive, ISlideShapes target)
    {
        var buffer = imageCache.GetOrDefault(primitive.Path) ?? throw new KeyNotFoundException($"No image data found for {primitive.Path}");

        using var stream = new MemoryStream(buffer);
        target.AddPicture(stream);
        var pic = target.OfType<IPicture>().Last();
        pic.X = primitive.Rectangle.X;
        pic.Y = primitive.Rectangle.Y ?? 0;
        pic.Width = primitive.Rectangle.Width;
        pic.Height = primitive.Rectangle.Height ?? primitive.Rectangle.Width;
    }

    private static void Draw(RectanglePrimitive primitive, ISlideShapes target)
    {
        target.AddRectangle(100, 100, 100, 100);
        var shape = target[^1];

        shape.X = primitive.Rectangle.X;
        shape.Y = primitive.Rectangle.Y ?? 0;
        shape.Width = primitive.Rectangle.Width;
        shape.Height = primitive.Rectangle.Height ?? primitive.Rectangle.Width;

        if (! primitive.Fill)
        {
            shape.Fill.SetNoFill();
        }
    }
}
