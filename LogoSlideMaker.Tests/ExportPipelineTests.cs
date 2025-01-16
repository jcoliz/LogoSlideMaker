using LogoSlideMaker.Export;
using LogoSlideMaker.Public;
using Moq;
using ShapeCrawler;

namespace LogoSlideMaker.Tests;

internal class ExportPipelineTests: TestsBase
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

    /// <summary>
    /// Scenario: Document title rendered into presentation
    /// </summary>
    [Test]
    public void RendersTitle()
    {
        // Given: A variant with a unique title
        var expected = "New document title";
        var variant = new Mock<IVariant>();        
        variant.Setup(x=>x.DocumentTitle).Returns(expected);
        variant.Setup(x=>x.Description).Returns([]);
        variant.Setup(x=>x.Notes).Returns([]);
        variant.Setup(x=>x.GeneratePrimitives(It.IsAny<ImageCache>())).Returns([]);

        // And: A presentation with a title block
        var presentation = new Presentation();
        var shapes = presentation.Slides[0].Shapes;
        shapes.AddShape(10,10,10,10);
        var shape = shapes[^1];
        shape.Name = "Title";
        shape.Text = "Old document title";

        // When: Rendering the variant to the presentation
        var renderer = new ExportRenderEngineEx(presentation, variant.Object, new(), null);
        renderer.Render();

        // Then: The title appears in the expected shape on the rendered slide
        shapes = presentation.Slides[^1].Shapes;
        var titleShape = shapes.TryGetByName<IShape>("Title")!;
        Assert.That(titleShape.Text,Is.EqualTo(expected));
    }
}