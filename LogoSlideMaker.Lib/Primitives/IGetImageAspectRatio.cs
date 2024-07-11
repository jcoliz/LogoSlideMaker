namespace LogoSlideMaker.Primitives;

/// <summary>
/// Provides the aspect ration of an image, given its by path
/// </summary>
public interface IGetImageAspectRatio
{
    /// <summary>
    /// True if we have an aspect ratio for this image
    /// </summary>
    /// <param name="imagePath">Path to image, as specified in logo</param>
    public bool Contains(string imagePath);

    /// <summary>
    /// Get the aspect ratio of a given image (width/height)
    /// </summary>
    /// <param name="imagePath">Path to image, as specified in logo</param>
    public decimal GetAspectRatio(string imagePath);
}
