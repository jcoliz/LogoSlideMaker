using System.Reflection;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Export;
using Tomlyn;

namespace LogoSlideMaker.Tests;

public class ExportPipelineTests
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

    // TODO: DRY
    private static Definition Load(string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x=>x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        var sr = new StreamReader(stream!);
        var toml = sr.ReadToEnd();
        var definitions = Toml.ToModel<Definition>(toml);

        return definitions;
    }

}