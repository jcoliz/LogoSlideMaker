using System.Reflection;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Export;
using Tomlyn;

namespace LogoSlideMaker.Tests;

/// <summary>
/// Common base class for tests
/// </summary>
/// <remarks>
/// Contains shared logic used by many tests
/// </remarks>

public abstract class TestsBase
{
    /// <summary>
    /// Load this definition file from embedded resources
    /// </summary>
    /// <param name="filename">Name of the file</param>
    /// <returns>Defintion contained within that file</returns>
    protected static Definition Load(string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x=>x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        var sr = new StreamReader(stream!);
        var toml = sr.ReadToEnd();
        var definitions = Toml.ToModel<Definition>(toml);

        return definitions;
    }

    protected static Stream GetStream(string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);

        return stream!;
    }

}