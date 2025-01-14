using ShapeCrawler;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;

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
    public void Render(Presentation pres, IVariant variant, string? dataVersion)
    {
        // Generate primitives, but we are only goint to render 'base' primitives to PPTX
        var primitives = variant
            .GeneratePrimitives(imageCache)
            .Where(x => x.Purpose == PrimitivePurpose.Base)
            .ToList();

        var copyingSlide = pres.Slides[variant.Source];
        pres.Slides.Add(copyingSlide);
        var slide = pres.Slides[^1];

        //
        // Update Description Field
        //

        SetDescription(variant.Description, slide.Shapes);

        //
        // Set Slide Notes
        //

        List<string> notes = [$"Updated: {DateTime.Now:M/dd/yyyy h:mm tt K}"];
        if (dataVersion is not null)
        {
            notes.Add($"Version: {dataVersion}");
        }
        notes.AddRange(variant.Notes);
        slide.AddNotes(notes);

        //
        // Draw primitives
        //

        TextStyles = variant.TextStyles; // Not the best way to get this in, is it??
        foreach (var p in primitives)
        {
            DrawEx(p, slide.Shapes);
        }
    }

    public void Render(Presentation pres, SlideLayout layout, string? dataVersion, IEnumerable<Primitive> primitives)
    {
        var copyingSlide = pres.Slides[layout.Variant.Source];
        pres.Slides.Add(copyingSlide);
        var slide = pres.Slides[^1];

        //
        // Update Description Field
        //

        SetDescription(layout.Variant.Description, slide.Shapes);

        //
        // Set Slide Notes
        //

        List<string> notes = [$"Updated: {DateTime.Now:M/dd/yyyy h:mm tt K}"];
        if (dataVersion is not null)
        {
            notes.Add($"Version: {dataVersion}");
        }
        notes.Add($"Logo count: {layout.Logos.Count(y => y.Logo != null)}");
        slide.AddNotes(notes);

        //
        // Draw primitives
        //

        foreach (var p in primitives)
        {
            Draw(p, slide.Shapes);
        }
    }

    private static void SetDescription(ICollection<string> description, ISlideShapes target)
    {
        // Fill in description field
        var num_description_lines = description.Count;
        if (num_description_lines > 0)
        {
            var description_box = target.TryGetByName<IShape>("Description");
            if (description_box is not null)
            {
                var tf = description_box.TextBox;

                // Ensure there are enough paragraphs to insert text
                while (tf.Paragraphs.Count < num_description_lines)
                {
                    tf.Paragraphs.Add();
                }

                var queue = new Queue<string>(description);
                foreach(var para in tf.Paragraphs)
                {
                    if (queue.Count == 0)
                    {
                        break;
                    }
                    para.Text = queue.Dequeue();
                }
            }
        }        
    }

    private static void SetDescription(IVariant variant, ISlideShapes target)
    {
        // Fill in description field
        var num_description_lines = variant.Description.Count;
        if (num_description_lines > 0)
        {
            var description_box = target.TryGetByName<IShape>("Description");
            if (description_box is not null)
            {
                var tf = description_box.TextBox;

                // Ensure there are enough paragraphs to insert text
                while (tf.Paragraphs.Count < num_description_lines)
                {
                    tf.Paragraphs.Add();
                }

                var queue = new Queue<string>(variant.Description);
                foreach (var para in tf.Paragraphs)
                {
                    if (queue.Count == 0)
                    {
                        break;
                    }
                    para.Text = queue.Dequeue();
                }
            }
        }
    }

    private void DrawEx(Primitive primitive, ISlideShapes target)
    {
        switch (primitive)
        {
            case TextPrimitive text:
                DrawEx(text, target);
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

    private void DrawEx(TextPrimitive primitive, ISlideShapes target)
    {
        if (primitive.Style == TextSyle.Invisible)
            return;

        target.AddShape(100, 100, 100, 100);
        var shape = target[^1];

        shape.X = primitive.Rectangle.X;
        shape.Y = primitive.Rectangle.Y ?? 0;
        shape.Width = primitive.Rectangle.Width;
        shape.Height = primitive.Rectangle.Height ?? primitive.Rectangle.Width;

        var tf = shape.TextBox;
        tf.Text = primitive.Text;
        tf.LeftMargin = 0;
        tf.RightMargin = 0;
        var font = tf.Paragraphs[0].Portions[0].Font;

        if (font is not null)
        {
            if (TextStyles.TryGetValue(primitive.Style, out var textStyle))
            {
                font.Size = textStyle.FontSize;
                font.LatinName = textStyle.FontName;
                font.Color.Update(textStyle.FontColor);
            }
            else
            {
                throw new Exception($"Unsupported text style {primitive.Style}");
            }
        }

        shape.Fill.SetNoFill();
        shape.Outline.SetNoOutline();
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
        if (primitive.Style == TextSyle.Invisible)
            return;

        target.AddShape(100, 100, 100, 100);
        var shape = target[^1];

        shape.X = primitive.Rectangle.X;
        shape.Y = primitive.Rectangle.Y ?? 0;
        shape.Width = primitive.Rectangle.Width;
        shape.Height = primitive.Rectangle.Height ?? primitive.Rectangle.Width;

        var tf = shape.TextBox;
        tf.Text = primitive.Text;
        tf.LeftMargin = 0;
        tf.RightMargin = 0;
        var font = tf.Paragraphs[0].Portions[0].Font;

        if (font is not null)
        {
            switch(primitive.Style)
            {
                // TODO: Could expand config to define styles as its own config type
                // then look them up here in a dictionary.
                case TextSyle.Logo:
                    font.Size = config.FontSize;
                    font.LatinName = config.FontName;
                    font.Color.Update(config.FontColor);
                    break;
                case TextSyle.BoxTitle:
                    font.Size = config.TitleFontSize;
                    font.LatinName = config.TitleFontName;
                    font.Color.Update(config.TitleFontColor);
                    break;
                default:
                    throw new Exception($"Unsupported text style {primitive.Style}");
            }
        }

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
        target.AddShape(100, 100, 100, 100);
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

    private IReadOnlyDictionary<TextSyle, ITextStyle> TextStyles = new Dictionary<TextSyle, ITextStyle>();
}
