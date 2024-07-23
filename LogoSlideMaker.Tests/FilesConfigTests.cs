using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using System.Reflection;
using Tomlyn;

namespace LogoSlideMaker.Tests;

public class FilesConfigTests
{
    /// <summary>
    /// Scenario: Definition with include.logos loads ok
    /// </summary>
    [Test]
    public void LoadsOk()
    {
        // When: Loading a definition file with "include.logos" line
        var definition = Load("include-logos.toml");

        // Then: Logos filename shows up in the layout
        Assert.That(definition.Files.Include.Logos, Is.EqualTo("two-pages.toml"));
    }

    private static Definition Load(string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        var sr = new StreamReader(stream!);
        var toml = sr.ReadToEnd();
        var definitions = Toml.ToModel<Definition>(toml);

        return definitions;
    }
}
