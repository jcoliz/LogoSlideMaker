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

        Assert.That(layout, Is.Empty);
    }
}
