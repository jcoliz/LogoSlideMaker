using System.Reflection;
using System.Threading.Tasks;
using LogoSlideMaker.Export;
using LogoSlideMaker.Public;
using Moq;
using ShapeCrawler;
using ShapeCrawler.Drawing;

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

    [Test]
    public async Task ImageCropCorrectSize()
    {
        // Given: A logo with excess imagery we don't want
        // And: Specifying 'crop` dimensions in the definition
        var definition = Loader.Load(GetStream("crop-export.toml")) as PublicDefinition;

        // And: A presentation
        var presentation = new Presentation();

        // When: Rendering the variant to the presentation
        var imageCache = new ImageCache() { ImagesAssembly = Assembly.GetExecutingAssembly() };
        await imageCache.LoadAsync(definition!.ImagePaths);
        var renderer = new ExportRenderEngineEx(presentation, definition!.Variants[0], imageCache , null);
        renderer.Render();

        presentation.SaveAs("crop-export.pptx");

        // Then: Size of image is as expected
        var shape = presentation.Slides[^1].Shapes[0] as IPicture;
        Assert.That(shape!.Width,Is.EqualTo(100m).Within(0.01m));
        Assert.That(shape.Height,Is.EqualTo(100m).Within(0.01m));

        // And: Cropping rectangle is as expected
        Assert.That(shape.Crop,Is.EqualTo(new CroppingFrame(0,75,0,0)));

        // And: Size/cropping of image with left-edge cropping is as expected 
        shape = presentation.Slides[^1].Shapes[2] as IPicture;
        Assert.That(shape!.Width, Is.EqualTo(100m).Within(0.01m));
        Assert.That(shape.Height, Is.EqualTo(100m).Within(0.01m));
        Assert.That(shape.Crop, Is.EqualTo(new CroppingFrame(75, 0, 0, 0)));

        // And: Size/cropping of image with left-right-edge cropping is as expected 
        shape = presentation.Slides[^1].Shapes[4] as IPicture;
        Assert.That(shape!.Width, Is.EqualTo(141.42m).Within(0.01m));
        Assert.That(shape.Height, Is.EqualTo(70.71m).Within(0.01m));
        Assert.That(shape.Crop, Is.EqualTo(new CroppingFrame(25, 25, 0, 0)));

        // And: Size/cropping of image with all-edge cropping is as expected 
        shape = presentation.Slides[^1].Shapes[6] as IPicture;
        Assert.That(shape!.Width, Is.EqualTo(200m).Within(0.01m));
        Assert.That(shape.Height, Is.EqualTo(50m).Within(0.01m));
        Assert.That(shape.Crop, Is.EqualTo(new CroppingFrame(25, 25, 25, 25)));
    }

    [Test]
    public async Task LogoCornerCorrectSize()
    {
        // Given: A definition with a logo having specified corner radius
        var definition = Loader.Load(GetStream("corner-radius.toml")) as PublicDefinition;

        // And: A presentation
        var presentation = new Presentation();

        // When: Rendering the variant to the presentation
        var imageCache = new ImageCache() { ImagesAssembly = Assembly.GetExecutingAssembly() };
        await imageCache.LoadAsync(definition!.ImagePaths);
        var renderer = new ExportRenderEngineEx(presentation, definition!.Variants[0], imageCache , null);
        renderer.Render();

        presentation.SaveAs("corner-radius.pptx");

        // Then: Cropping rectangle is as expected
        // Expected corner size to be ...
        // Shape is 200x50 (4:1), corner radius is 10, corner size is 10/(50/2)
        var shape = presentation.Slides[^1].Shapes[0] as IPicture;
        Assert.That(shape!.CornerSize,Is.EqualTo(40));
    }

    [TestCase("four.webp")]
    [TestCase("four.bmp")]
    [TestCase("four.gif")]
    public async Task LoadImageFormats(string filename)
    {
        // When: Loading image paths containing WebP image
        var imageCache = new ImageCache() { ImagesAssembly = Assembly.GetExecutingAssembly() };
        await imageCache.LoadAsync([filename]);

        // Then: Aspect ratio is returned as expected
        var actual = imageCache.GetAspectRatio(filename);
        Assert.That(actual,Is.EqualTo(4));
    }

    [Test]
    public async Task RenderImageFormats()
    {
        // Given: A definition with logo having various limage formats
        var definition = Loader.Load(GetStream("webp.toml")) as PublicDefinition;

        // And: A presentation
        var presentation = new Presentation();

        // When: Rendering the variant to the presentation
        var imageCache = new ImageCache() { ImagesAssembly = Assembly.GetExecutingAssembly() };
        await imageCache.LoadAsync(definition!.ImagePaths);
        var renderer = new ExportRenderEngineEx(presentation, definition!.Variants[0], imageCache , null);
        renderer.Render();

        presentation.SaveAs("webp.pptx");

        // Then: Images are added as expected (?)

    }    
}
