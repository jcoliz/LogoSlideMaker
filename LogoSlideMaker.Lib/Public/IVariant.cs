using LogoSlideMaker.Primitives;

namespace LogoSlideMaker.Public;

public interface IVariant
{
    string Name { get; }
    ICollection<string> Description { get; }
    ICollection<Primitive> GeneratePrimitives(IGetImageAspectRatio bitmaps);
}
