using LogoSlideMaker.Configure;
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

    [Test]
    public void Merge()
    {
        // Given: A definition file with "include.logos" line
        var definition = Load("include-logos.toml");

        // When: Merging in the logos
        var included = Load(definition.Files.Include!.Logos!);
        definition.IncludeLogosFrom(included);

        // Then: Definition contians the included logos
        Assert.That(definition.Logos, Has.Count.EqualTo(4));
    }

    [Test]
    public void DontMergeDuplicates()
    {
        // Given: A definition file with "include.logos" line, where the included file has
        // a duplicate logo key
        var definition = Load("include-logos-duplicate.toml");

        // When: Merging in the logos
        var included = Load(definition.Files.Include!.Logos!);
        definition.IncludeLogosFrom(included);

        // Then: Definition contains the original logo for the duplicate (was not overriden by the include)
        Assert.That(definition.Logos["zero"], Has.Property("Title").EqualTo("Original Zero!"));
    }

    [Test]
    public void AllowOverwriteTextWidth()
    {
        // Given: A definition file with "include.logos" line, where the included file has
        // a duplicate logo key which ONLY specifies text width
        var definition = Load("include-logos-duplicate.toml");

        // When: Merging in the logos
        var included = Load(definition.Files.Include!.Logos!);
        definition.IncludeLogosFrom(included);

        // Then: Definition contains the title of the new logo
        Assert.That(definition.Logos["one"], Has.Property("Title").EqualTo("one"));

        // Then: And the text width is as-specified in our original definition
        Assert.That(definition.Logos["one"], Has.Property("TextWidth").EqualTo(100m));

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
