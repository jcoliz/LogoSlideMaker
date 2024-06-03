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
    }

}
