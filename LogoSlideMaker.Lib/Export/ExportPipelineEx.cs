using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
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
public class ExportPipelineEx
{
    public ExportPipelineEx(IDefinition __definition)
    {
        definition = __definition;
        imageCache = new();
        renderEngine = new(/* TODO Need to get render data here*/new(), imageCache);
    }

    public async Task LoadAndMeasureAsync(string? basePath)
    {
        imageCache.BaseDirectory = basePath;
        await imageCache.LoadAsync(definition.ImagePaths);
    }

    public void Save(Stream? templateStream, string outputPath, string? dataVersion)
    {
        // Open template or create new presentation
        var pres = templateStream is not null ? new Presentation(templateStream) : new Presentation();

        // Retain number of slides in template
        var numTemplateSlides = pres.Slides.Count;

        // RUn each variant through pipeline
        foreach (var variant in definition.Variants)
        {
            // Render
            renderEngine.Render(pres, variant, dataVersion);
        }

        // Delete template slides off top of result
        var removeSlides = pres.Slides.Take(numTemplateSlides).ToArray();
        foreach(var slide in removeSlides)
        {
            pres.Slides.Remove(slide);
        }

        // Save the resulting presentation
        pres.SaveAs(outputPath);
    }

    private readonly IDefinition definition;
    private readonly ImageCache imageCache;
    private readonly ExportRenderEngine renderEngine;
}
