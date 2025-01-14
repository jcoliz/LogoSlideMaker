using LogoSlideMaker.Export;
using LogoSlideMaker.Public;
using ShapeCrawler;

namespace LogoSlideMaker.Tests;

public class ExportPipelineTests: TestsBase
{
    /// <summary>
    /// Scenario: Rendering functions OK
    /// </summary>
    [Test]
    public void RendersOk()
    {
        // Given: A definition with one variant using a template with one slide
        var definition = Loader.Load(GetStream("simple.toml"));

        // And: A presentation to export to
        var presentation = new Presentation();

        // When: Exporting it
        var renderer = new ExportRenderEngineEx(presentation, definition.Variants[0], new(), null);
        renderer.Render();

        // Then: Result has two slide (because we haven't chopped off the original slides yet)
        Assert.That(presentation.Slides,Has.Count.EqualTo(2));

        // And: Resulting slide has contents as specified in definition
        Assert.That(presentation.Slides[1].Shapes,Has.Count.EqualTo(4));
    }
}