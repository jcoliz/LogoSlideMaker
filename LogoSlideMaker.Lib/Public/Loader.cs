using DocumentFormat.OpenXml.InkML;
using DocumentFormat.OpenXml.Wordprocessing;
using LogoSlideMaker.Configure;
using Tomlyn;

namespace LogoSlideMaker.Public;

public static class Loader
{
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
}
