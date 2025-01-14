using LogoSlideMaker.Primitives;

namespace LogoSlideMaker.Tests.Helpers;

internal class TestImageSource : IGetImageAspectRatio
{
    public bool Contains(string imagePath)
    {
        return true;
    }

    public decimal GetAspectRatio(string imagePath)
    {
        return 1;
    }
}
