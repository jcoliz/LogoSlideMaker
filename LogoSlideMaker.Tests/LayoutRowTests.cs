using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;

namespace LogoSlideMaker.Tests;

/// <summary>
/// Rows feature: User can lay out logos in rows of evenly-spaced logos
/// </summary>
public class LayoutRowTests
{    
    [Test]
    public void Empty()
    {
        var definition = new Definition();
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes, Has.Length.EqualTo(0));
    }

    [Test]
    public void One()
    {
        var definition = new Definition() 
        {
            Rows = 
            [
                new Row()
                {
                    Logos = [ "test" ]
                }

            ],
            Logos = 
            { 
                { "test", new Logo() } 
            },

        };
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes, Has.Length.EqualTo(1));
        Assert.That(layout.Boxes[0].Logos, Has.Length.EqualTo(1));
    }

    [Test]
    public void Five()
    {
        var definition = new Definition() 
        {
            Rows = 
            [
                new Row()
                {
                    Logos = [ "test", "test" , "test" , "test" , "test"  ]
                }

            ],
            Logos = 
            { 
                { "test", new Logo() } 
            },

        };
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes, Has.Length.EqualTo(1));
        Assert.That(layout.Boxes[0].Logos, Has.Length.EqualTo(5));
    }

    [Test]
    public void LayoutTakesRowYPosition()
    {
        var definition = new Definition() 
        {
            Rows = 
            [
                new Row()
                {
                    Logos = [ "test", "test" , "test" , "test" , "test"  ],
                    YPosition = 5.0m
                }

            ],
            Logos = 
            { 
                { "test", new Logo() } 
            },

        };
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes[0].Logos, Has.All.With.Property("Y").EqualTo(5.0m));
    }

    /// <summary>
    /// First and last icons on row are pushed to the fulllest left and right of the row
    /// </summary>
    [Test]
    public void XPositionsAtExtents()
    {
        var definition = new Definition() 
        {
            Rows = 
            [
                new Row()
                {
                    Logos = [ "test", "test" , "test" , "test" , "test"  ],
                    XPosition = 5.0m,
                    Width = 10.0m
                }

            ],
            Logos = 
            { 
                { "test", new Logo() } 
            },

        };
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes[0].Logos[0], Has.Property("X").EqualTo(5.0m));
        Assert.That(layout.Boxes[^1].Logos[^1], Has.Property("X").EqualTo(15.0m));
    }

    /// <summary>
    /// Icons are spaced evenly across the row
    /// </summary>
    [Test]
    public void XPositionsSpaced()
    {
        var definition = new Definition() 
        {
            Rows = 
            [
                new Row()
                {
                    Logos = [ "test", "test" , "test" , "test" , "test"  ],
                    XPosition = 5.0m,
                    Width = 10.0m
                }

            ],
            Logos = 
            { 
                { "test", new Logo() } 
            },

        };
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes[0].Logos[1], Has.Property("X").EqualTo(7.5m));
        Assert.That(layout.Boxes[0].Logos[2], Has.Property("X").EqualTo(10.0m));
        Assert.That(layout.Boxes[0].Logos[3], Has.Property("X").EqualTo(12.5m));
    }

    /// <summary>
    /// Icons are spaced evenly across the row AS IF there were `mincolumns` columns
    /// </summary>
    [Test]
    public void MinColumns()
    {
        var definition = new Definition() 
        {
            Rows = 
            [
                new Row()
                {
                    Logos = [ "test", "test" ],
                    XPosition = 5.0m,
                    Width = 10.0m,
                    MinColumns = 5,

                }

            ],
            Logos = 
            { 
                { "test", new Logo() } 
            },

        };
        var variant = new Variant();
        var engine = new LayoutEngine(definition, variant);
        var layout = engine.CreateSlideLayout();

        Assert.That(layout.Boxes[0].Logos[0], Has.Property("X").EqualTo(5.0m));
        Assert.That(layout.Boxes[0].Logos[1], Has.Property("X").EqualTo(7.5m));
    }
}
