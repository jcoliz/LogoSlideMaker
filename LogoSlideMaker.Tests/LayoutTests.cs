using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;

namespace LogoSlideMaker.Tests;

public class LayoutTests
{
    [Test]
    public void Empty()
    {
        var definition = new Definition();
        var variant = new Variant();
        var layout = new Layout.Layout(definition, variant);

        layout.Populate();

        Assert.That(layout, Is.Empty);
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
        var layout = new Layout.Layout(definition, variant);

        layout.Populate();

        Assert.That(layout, Has.Count.EqualTo(1));
        Assert.That(layout.First().Logos, Has.Length.EqualTo(1));
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
        var layout = new Layout.Layout(definition, variant);

        layout.Populate();

        Assert.That(layout, Has.Count.EqualTo(1));
        Assert.That(layout.First().Logos, Has.Length.EqualTo(5));
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
        var layout = new Layout.Layout(definition, new Variant());

        layout.Populate();

        Assert.That(layout.First().Logos, Has.All.With.Property("Y").EqualTo(5.0m));
    }

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
        var layout = new Layout.Layout(definition, new Variant());

        layout.Populate();

        Assert.That(layout.First().Logos[0], Has.Property("X").EqualTo(5.0m));
        Assert.That(layout.Last().Logos[^1], Has.Property("X").EqualTo(15.0m));
    }

}
