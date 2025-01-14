using DocumentFormat.OpenXml.InkML;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Models;

namespace LogoSlideMaker.Tests;

/// <summary>
/// Boxes feature: User can group logos into boxes with evenly-spaced space rows
/// </summary>
internal class LayoutBoxTests: TestsBase
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
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: Top of logo is aligned to outer container plus padding
        Assert.That(layout.Logos[0].Y, Is.EqualTo(100+10+5m/2));
        Assert.That(layout.Logos[^1].Y, Is.EqualTo(100+10+5m/2));

        // Then: Left of left-most text box is aligned to outer container plus padding
        Assert.That(layout.Logos[0].X, Is.EqualTo(100+10+40m/2));

        // Then: Left of right-most text box is aligned to outer container plus padding
        Assert.That(layout.Logos[^1].X, Is.EqualTo(100+1000-10-40m/2));
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
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: Top of logo in second box is aligned to outer container plus padding, plus box and line spacing
        Assert.That(layout.Logos[5].Y, Is.EqualTo(100+10+5m/2+200+150));
    }

    [Test]
    public void AutoFlow()
    {
        // Given: A box with unbalanced rows and autoflow set to true
        var definition = Load("auto-flow.toml");

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: Logos are evenly distributed amongst rows
        Assert.That(layout.Logos[0..3], Has.All.Property("Y").EqualTo(0));
        Assert.That(layout.Logos[3..6], Has.All.Property("Y").EqualTo(10));
    }

    [Test]
    public void AutoFlowMinColumns()
    {
        // Given: A box with unbalanced rows and autoflow set to true, and min-columns set
        // to something which will still cause imbalance (here, 4)
        var definition = Load("auto-flow.toml");

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: First min_columns (4) logos on first line 
        Assert.That(layout.Logos[6..10], Has.All.Property("Y").EqualTo(20));

        // And: Remaining logos are on second line 
        Assert.That(layout.Logos[10..12], Has.All.Property("Y").EqualTo(30));
    }

    [Test]
    public void AutoFlowMinColumnsTags()
    {
        // Given: A box with unbalanced rows and autoflow set to true, and min-columns set
        // to something which will still cause imbalance (here, 6), and there are some
        // logos excluded by tags
        var definition = Load("auto-flow.toml");

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: First min_columns (6) logos on first line 
        Assert.That(layout.Logos[12..18], Has.All.Property("Y").EqualTo(40));

        // And: Second min_columns logos on second line 
        Assert.That(layout.Logos[18..24], Has.All.Property("Y").EqualTo(50));
    }

    [Test]
    public void NoAutoFlow()
    {
        // Given: A box with unbalanced rows and autoflow set to FALSE
        var definition = Load("auto-flow.toml");
        definition.Boxes[0].AutoFlow = false;

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: First row of logos all on same line
        Assert.That(layout.Logos[0..5], Has.All.Property("Y").EqualTo(0));
    }

    [Test]
    public void BoxTitles()
    {
        // Given: A layout with a title
        var definition = Load("render-title.toml");

        // When: Creating and populating these into a layout
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        // Then: There is a text layout added
        Assert.That(layout.Text, Has.Length.EqualTo(1));

        // And: The text matches expectations
        Assert.That(layout.Text[0].Text, Is.EqualTo("Left side"));

        // And: The position matches expectations
        Assert.That(layout.Text[0].Position, Is.EqualTo(new Edge()
        {
            X = 2,
            Y = 3,
            Length = 4,
            Kind = EdgeKind.Bottom
        }));
    }
}
