using System.Reflection;
using LogoSlideMaker.Configure;
using Tomlyn;

namespace LogoSlideMaker.Tests;

/// <summary>
/// Boxes feature: User can group logos into boxes with evenly-spaced space rows
/// </summary>
public class LayoutBoxTests
{    
    [Test]
    public void Padding()
    {
        // Given: A single box with outer dimensions
        // And: padding, iconsize, and text width sent
        var definition = new Definition() 
        {
            Boxes  = 
            [
                new Box()
                {
                    Logos = { { 1, [ "test", "test" , "test" , "test" , "test" ] } },
                    Outer = new Rectangle() { X = 100, Y = 100, Width = 1000 }
                }
            ],
            Logos = 
            { 
                { "test", new Logo() }
            },
            Layout = new Config() { Padding = 10 },
            Render = new RenderConfig() { IconSize = 5, TextWidth = 40 }
        };

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Top of logo is aligned to outer container plus padding
        Assert.That(layout.First().Logos.First().Y, Is.EqualTo(100+10+5m/2));
        Assert.That(layout.First().Logos.Last().Y, Is.EqualTo(100+10+5m/2));

        // Then: Left of left-most text box is aligned to outer container plus padding
        Assert.That(layout.First().Logos.First().X, Is.EqualTo(100+10+40m/2));

        // Then: Left of right-most text box is aligned to outer container plus padding
        Assert.That(layout.First().Logos.Last().X, Is.EqualTo(100+1000-10-40m/2));
    }

    [Test]
    public void Padding2ndRow()
    {
        // Given: Two boxes with outer dimensions, but 2nd box has no Y outer dimension
        // And: padding, iconsize, and text width sent
        var definition = new Definition() 
        {
            Boxes  = 
            [
                new Box()
                {
                    Logos = { { 1, [ "test", "test" , "test" , "test" , "test" ] } },
                    Outer = new Rectangle() { X = 100, Y = 100, Width = 1000 }
                },
                new Box()
                {
                    Logos = { { 1, [ "test", "test" , "test" , "test" , "test" ] } },
                    Outer = new Rectangle() { X = 100, Width = 1000 }
                }
            ],
            Logos = 
            { 
                { "test", new Logo() }
            },
            Layout = new Config() { Padding = 10, LineSpacing = 200, BoxSpacing = 150 },
            Render = new RenderConfig() { IconSize = 5, TextWidth = 40 }
        };

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var layout = new Layout.Layout(definition, variant);
        layout.Populate();

        // Then: Top of logo in second box is aligned to outer container plus padding, plus box and line spacing
        Assert.That(layout.Last().Logos.First().Y, Is.EqualTo(100+10+5m/2+200+150));
    }

    [Test]
    public void AutoFlow()
    {
        // Given: A box with unbalanced rows and autoflow set to true
        var definition = Load("auto-flow.toml");

        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, new Variant());
        layout.Populate();

        // Then: Logos are evenly distributed amongst rows
        Assert.That(layout.First().Logos[0..2], Has.All.Property("Y").EqualTo(0));
        Assert.That(layout.First().Logos[3..], Has.All.Property("Y").EqualTo(10));
    }

    [Test]
    public void NoAutoFlow()
    {
        // Given: A box with unbalanced rows and autoflow set to FALSE
        var definition = Load("auto-flow.toml");
        definition.Boxes[0].AutoFlow = false;

        // When: Creating and populating these into a layout
        var layout = new Layout.Layout(definition, new Variant());
        layout.Populate();

        // Then: First row of logos all on same line
        Assert.That(layout.First().Logos[0..^1], Has.All.Property("Y").EqualTo(0));
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
