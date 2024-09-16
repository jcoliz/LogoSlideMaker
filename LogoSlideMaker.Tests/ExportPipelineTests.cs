using LogoSlideMaker.Export;

namespace LogoSlideMaker.Tests;

public class ExportPipelineTests: TestsBase
{
    /// <summary>
    /// Scenario: Template slides removed from output
    /// </summary>
    [Test]
    public void TemplateSlidesRemoved()
    {
        // Given: A definition with one variant using a template with one slide
        var definition = Load("simple.toml");

        // When: Exporting it
        var pipeline = new ExportPipeline(definition);
        var presentation = pipeline.RenderAll(null, null);

        // Then: Result has only one slide
        Assert.That(presentation.Slides,Has.Count.EqualTo(1));

        // And: Resulting slide has contents as specified in definition
        Assert.That(presentation.Slides[0].Shapes,Has.Count.EqualTo(4));
    }
}