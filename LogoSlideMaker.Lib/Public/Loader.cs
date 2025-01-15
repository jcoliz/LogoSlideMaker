using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using LogoSlideMaker.Configure;
using Tomlyn;

namespace LogoSlideMaker.Public;

/// <summary>
/// Manages creation of document definitions
/// </summary>
public static class Loader
{
    /// <summary>
    /// Load a document definition from a stream
    /// </summary>
    /// <param name="stream">Data containing a TOML definition for a document</param>
    /// <param name="basePath">Path where the file was located (optional)</param>
    /// <returns>The loaded document definition</returns>
    /// <exception cref="Exception">Loading errors</exception>
    public static IDefinition Load(Stream stream, string? basePath = null)
    {
        var sr = new StreamReader(stream);
        var toml = sr.ReadToEnd();
        var definition = Toml.ToModel<Definition>(toml) ?? throw new Exception("Unable to parse");

        if (basePath is not null && !string.IsNullOrWhiteSpace(definition.Files.Include.Logos))
        {
            var logopath = Path.Combine(basePath, definition.Files.Include.Logos);

            using var logostream = File.OpenRead(logopath);
            using var logosr = new StreamReader(logostream);
            var logotoml = logosr.ReadToEnd();
            var logos = Toml.ToModel<Definition>(logotoml);

            definition.IncludeLogosFrom(logos);
        }

        return new PublicDefinition(definition);
    }

    /// <summary>
    /// Returns an empty document defintion
    /// </summary>
    /// <remarks>
    /// Useful to avoid having a null reference for 'empty' doc
    /// </remarks>
    /// <returns>Empty document definition</returns>
    public static IDefinition Empty()
    {
        return new PublicDefinitionEmpty();
    }
}
