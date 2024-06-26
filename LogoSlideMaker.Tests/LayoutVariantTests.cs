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
    /// Scenario: By default, logos with tags are not included
    /// </summary>
    [Test]
    public void NoTags()
    {
        // Given: Some logos with tags, and some without
        var definition = Load("tags.toml");

        // And: A variant with no tags
        var variant = new Variant();
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Only the logos with no tags are included
        Assert.That(layout.First().Logos, Has.Length.EqualTo(1));
    }

    /// <summary>
    /// Scenario: Variant with one tag gets all matching logos and un-tagged
    /// </summary>
    [Test]
    public void OneTag()
    {
        // Given: Some logos with tags, and some without
        var definition = Load("tags.toml");

        // And: A variant with no one tag
        var variant = new Variant() { Include = [ "t1"] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Only the logos with that tag and those with no tags are included
        Assert.That(layout.First().Logos.Select(x=>x.Logo!.Title), Is.EqualTo(new string[] { "zero", "one", "two" } ));
    }

    /// <summary>
    /// Scenario: Variant with two tags gets all matching logos and un-tagged
    /// </summary>
    [Test]
    public void TwoTags()
    {
        // Given: Some logos with tags, and some without
        var definition = Load("tags.toml");

        // And: A variant with no one tag
        var variant = new Variant() { Include = [ "t2", "t3" ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Only the logos with that tag and those with no tags are included
        Assert.That(layout.First().Logos.Select(x=>x.Logo!.Title), Is.EqualTo(new string[] { "zero", "two", "three" } ));
    }

    /// <summary>
    /// Scenario: User can tag a single entry, and will only be included in variants with those tags
    /// </summary>
    [Test]
    public void LogoTags()
    {
        // Given: A logo with a tag specified in the row
        var definition = Load("tags.toml");

        // And: A variant with that tag
        var variant = new Variant() { Include = [ "t4" ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Only the logos with that tag and those with no tags are included
        Assert.That(layout.First().Logos.Select(x=>x.Logo!.Title), Is.EqualTo(new string[] { "zero", "two", "zero" } ));
    }

    /// <summary>
    /// Scenario: Logo with not-tag is excluded when variant has that tag
    /// </summary>
    [Test]
    public void LogoNotTags()
    {
        // Given: A logo with a not-tag specified in the row
        var definition = Load("not-tags.toml");

        // And: A variant with that tag
        var variant = new Variant() { Include = [ "t2" ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Only the logos with that tag and those with no tags are included
        Assert.That(layout.First().Logos.Select(x=>x.Logo!.Title), Is.EqualTo(new string[] { "zero", "three" } ));
    }

    /// <summary>
    /// Scenario: Logo with not-tag is included when variant has no tags
    /// </summary>
    [Test]
    public void LogoNotTagsNoTags()
    {
        // Given: A logo with a not-tag specified in the row
        var definition = Load("not-tags.toml");

        // And: A variant with no tags
        var variant = new Variant();
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Only the logos with that tag and those with no tags are included
        Assert.That(layout.First().Logos.Select(x=>x.Logo!.Title), Is.EqualTo(new string[] { "zero", "zero" } ));
    }

    /// <summary>
    /// Scenario: Logo with tagged end command is ignored when variant has no tag
    /// </summary>
    [Test]
    public void LogoEndCommandIgnored()
    {
        // Given: A logo with a not-tag specified in the row
        var definition = Load("end-tag.toml");

        // And: A variant with no tags
        var variant = new Variant();
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: All logos includwed
        Assert.That(layout.First().Logos, Has.Length.EqualTo(6));
    }

    /// <summary>
    /// Scenario: Logo with tagged end command stops when variant has  tag
    /// </summary>
    [Test]
    public void LogoEndCommand()
    {
        // Given: A logo with an end command specified in the row
        var definition = Load("end-tag.toml");

        // And: A variant with the matching tag
        var variant = new Variant() { Include = [ "t1" ] };
        
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Stops when end tag encountered
        Assert.That(layout.First().Logos, Has.Length.EqualTo(2));
    }

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

    [Test]
    public void MaskingOnlySelected()
    {
        var definition = Load("masking-only.toml");
                
        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, definition.Variants[0]);
        layout.Populate();

        // Then: Has give logos
        Assert.That(layout.First().Logos, Has.Length.EqualTo(5));

        // And: Logo "three" is shown
        Assert.That(layout.First().Logos.Select(x=>x.Logo), Has.Some.With.Property("Title").EqualTo("three"));
    }

    [Test]
    /// <summary>
    /// Scenario: Can load file with chinese language annotations
    /// </summary>
    public void Chinese()
    {
        Load("chinese.toml");
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
