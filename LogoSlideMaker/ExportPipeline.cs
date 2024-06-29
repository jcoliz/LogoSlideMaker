using LogoSlideMaker.Cli.Services;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.Render;
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
        renderEngine = new(definition.Render);
    }

    public async Task LoadAndMeasureAsync(string basePath)
    {
        imageCache.BaseDirectory = basePath;
        await imageCache.LoadAsync(definition.Logos.Select(x => x.Value.Path));
    }

    public void Save(string? templatePath, string outputPath, string? dataVersion)
    {
        // Open template or create new presentation
        var pres = !string.IsNullOrWhiteSpace(templatePath) ? new Presentation(templatePath) : new Presentation();

        foreach (var variant in definition.Variants)
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
