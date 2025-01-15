using LogoSlideMaker.Configure;
using LogoSlideMaker.Models;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;
using ShapeCrawler;

namespace LogoSlideMaker.Export;

/// <summary>
/// This class encapsulates everything needed to export a definition
/// out to a powerpoint file.
/// </summary>
/// <remarks>
/// This is an attempt to package the exporting currently done in the
/// command-line tool, and make it accessible to the UI.
/// </remarks>
public static class ExportPipelineEx
{
    /// <summary>
    /// Export a document to a presentation file
    /// </summary>
    /// <param name="definition">Document definition</param>
    /// <param name="imageCache">Source of images</param>
    /// <param name="templateStream">Source of initial slides template (optional)</param>
    /// <param name="outputPath">Path to target output file</param>
    /// <param name="dataVersion">Version to annotate on slides (optional)</param>
    public static void Export(IDefinition definition, ImageCache imageCache, Stream? templateStream, string outputPath, string? dataVersion)
    {
        // Open template or create new presentation
        var pres = templateStream is not null ? new Presentation(templateStream) : new Presentation();

        // Retain number of slides in template
        var numTemplateSlides = pres.Slides.Count;

        // RUn each variant through pipeline
        foreach (var variant in definition.Variants)
        {
            var renderer = new ExportRenderEngineEx(pres, variant, imageCache, dataVersion);
            renderer.Render();
        }

        // Delete template slides off top of result
        var removeSlides = pres.Slides.Take(numTemplateSlides).ToArray();
        foreach(var slide in removeSlides)
        {
            pres.Slides.Remove(slide);
        }

        // Save the resulting presentation
        // TODO: Should save to a stream to improve testability
        pres.SaveAs(outputPath);
    }
}

internal class ExportRenderEngineEx(Presentation pres, IVariant variant, ImageCache imageCache, string? dataVersion)
{
    internal void Render()
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

        SetDescription(slide.Shapes);

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

        foreach (var p in primitives)
        {
            Draw(p, slide.Shapes);
        }
    }

    private void SetDescription(ISlideShapes target)
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
            if (variant.TextStyles.TryGetValue(primitive.Style, out var textStyle))
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
}