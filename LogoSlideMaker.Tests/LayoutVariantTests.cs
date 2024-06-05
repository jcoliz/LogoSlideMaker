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
    /// <summary>
    /// Scenario: Variant with unspecified pages displays only boxes with unspecified page
    /// </summary>
    [Test]
    public void OnlyUnspecifiedPage()
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

    /// <summary>
    /// Scenario: Variant with specified pages displays only boxes with specified page
    /// </summary>
    [Test]
    public void OnlySpecifiedPage()
    {
        // Given: Two boxes, one with unspecified page, one in page 1
        var definition = Load("pages.toml");
        
        // Given: A variant with a specified page
        var variant = new Variant() { Pages = [ 1 ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Has only one box of logos
        Assert.That(layout, Has.Count.EqualTo(1));

        // And: All logos are the "one" logo, the logo contained in the page 1 box
        Assert.That(layout.First().Logos.Select(x=>x.Logo), Has.All.With.Property("Title").EqualTo("one"));
    }

    /// <summary>
    /// Scenario: Variant with multiple specified pages displays all boxes with those pages
    /// </summary>
    [Test]
    public void MultiplePage()
    {
        // Given: Four boxes, one with unspecified page, one each in pages 1,2,3
        var definition = Load("two-pages.toml");
        
        // Given: A variant with two specified pages
        var variant = new Variant() { Pages = [ 1, 2 ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Has two boxes of logos
        Assert.That(layout, Has.Count.EqualTo(2));

        // And: Second box of logos are the "two" logo
        Assert.That(layout.Skip(1).First().Logos.Select(x=>x.Logo), Has.All.With.Property("Title").EqualTo("two"));
    }

    /// <summary>
    /// Scenario: All boxes display normally when pages are not mentioned at all
    /// </summary>
    [Test]
    public void AllWhenNoPages()
    {
        // Given: Two boxes, both with unspecified pages
        var definition = Load("nopages.toml");
        
        // Given: A variant with no specified page
        var variant = new Variant();
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Has two boxes of logos
        Assert.That(layout, Has.Count.EqualTo(2));

        // And: Second box of logos are the "one" logo
        Assert.That(layout.Skip(1).First().Logos.Select(x=>x.Logo), Has.All.With.Property("Title").EqualTo("one"));
    }

    [Test]
    [Explicit("Failing test for in-progress feature")]
    public void Masking()
    {
        var definition = Load("masking.toml");
                
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, definition.Variants[0]);
        layout.Populate();

        // Then: Has four logos
        Assert.That(layout.First().Logos, Has.Length.EqualTo(4));

        // And: Logo "two" is never shown
        Assert.That(layout.First().Logos.Select(x=>x.Logo), Has.None.With.Property("Title").EqualTo("two"));
    }

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
