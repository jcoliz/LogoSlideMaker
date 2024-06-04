using System.Reflection;
using LogoSlideMaker.Configure;
using Tomlyn;

namespace LogoSlideMaker.Tests;

/// <summary>
/// Variant feature: User can create multiple slides, each with a different 
/// subset of logos
/// </summary>
/// <remarks>
/// Logos can be filtered by tag or by page
/// </remarks>
public class LayoutVariantTests
{    
    [Test]
    [Explicit("Failing test for in-progress feature")]
    public void OnlyUnspecified()
    {
        // Given: Two boxes, one with unspecified page, one in page 1
        var definition = Load("pages.toml");
        
        // Given: A variant with no specified page
        var variant = new Variant();
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Has only one box of logos
        Assert.That(layout, Has.Count.EqualTo(1));

        // And: All logos are the "zero" logo, the logo contained in the box with unspecified page
        Assert.That(layout.First().Logos.Select(x=>x.Logo), Has.All.With.Property("Title").EqualTo("zero"));
    }

    [Test]
    [Explicit("Failing test for in-progress feature")]
    public void OnlySpecifiedPage()
    {
        // Given: Two boxes, one with unspecified page, one in page 1
        var definition = Load("pages.toml");
        
        // Given: A variant with no specified page
        var variant = new Variant() { Pages = [ 1 ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Has only one box of logos
        Assert.That(layout, Has.Count.EqualTo(1));

        // And: All logos are the "one" logo, the logo contained in the page 1 box
        Assert.That(layout.First().Logos.Select(x=>x.Logo), Has.All.With.Property("Title").EqualTo("one"));
    }

    private static Definition Load(string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x=>x.Contains(filename)).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        var sr = new StreamReader(stream!);
        var toml = sr.ReadToEnd();
        var definitions = Toml.ToModel<Definition>(toml);

        return definitions;
    }
}
