using LogoSlideMaker.Configure;
using Tomlyn;

namespace LogoSlideMaker.Public;

public static class Loader
{
    public static IDefinition Load(Stream stream)
    {
        var sr = new StreamReader(stream);
        var toml = sr.ReadToEnd();
        var definition = Toml.ToModel<Definition>(toml) ?? throw new Exception("Unable to parse");

        if (definition.Variants.Count == 0)
        {
            definition.Variants = [new Variant()];
        }

        return new PublicDefinition(definition);
    }
}
