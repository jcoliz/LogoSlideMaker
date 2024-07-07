using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;
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
public class ExportPipeline
{
    public ExportPipeline(Definition __definition)
    {
        definition = __definition;
        imageCache = new();
        primitivesEngine = new(definition.Render, imageCache);
        renderEngine = new(definition.Render, imageCache);
    }

    public async Task LoadAndMeasureAsync(string? basePath)
    {
        imageCache.BaseDirectory = basePath;
        await imageCache.LoadAsync(definition.Logos.Select(x => x.Value.Path));
    }

    public void Save(Stream? templateStream, string outputPath, string? dataVersion)
    {
        // Open template or create new presentation
        var pres = templateStream is not null ? new Presentation(templateStream) : new Presentation();

        // If there isn't a specified variant, default to an EMPTY one
        // TODO: Perhaps better to do this on LOAD
        var variants = definition.Variants.Count > 0 ? definition.Variants : [new()];

        // RUn each variant through pipeline
        foreach (var variant in variants)
        {
            // Layout
            var layout = new LayoutEngine(definition, variant).CreateSlideLayout();

            // Primitives
            var primitives = layout.Logos.SelectMany(primitivesEngine.ToPrimitives);

            // Render
            renderEngine.Render(pres, layout, dataVersion, primitives);
        }

        // Save the resulting presentation
        pres.SaveAs(outputPath);
    }

    private readonly Definition definition;
    private readonly ImageCache imageCache;
    private readonly PrimitivesEngine primitivesEngine;
    private readonly ExportRenderEngine renderEngine;
}
