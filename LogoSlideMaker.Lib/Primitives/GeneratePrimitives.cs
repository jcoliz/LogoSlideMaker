using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;

namespace LogoSlideMaker.Primitives;

/// <summary>
/// Provides the size of an image by path
/// </summary>
/// <remarks>
/// Would be more correct to return aspect ratio here, that's what we really care about
/// </remarks>
public interface IGetImageAspectRatio
{
    public bool Contains(string imagePath);
    /// <summary>
    /// Get the aspect ratio of a given image (width/height)
    /// </summary>
    /// <param name="imagePath">Path to image, as specified in logo</param>
    /// <returns>Size of given image, in arbitrary units</returns>
    public decimal GetAspectRatio(string imagePath);
}

/// <summary>
/// Generates simple drawing primitives to show a single logolayout
/// </summary>
/// <remarks>
/// These primitives could be drawn by any renderer
/// </remarks>
/// <param name="config">Rendering configuration to covern detailed sizing</param>
/// <param name="getLogoAspect">Provides image size for any given image path</param>
public class PrimitivesEngine(RenderConfig config, IGetImageAspectRatio getLogoAspect)
{
    /// <summary>
    /// Breakdown a single logolayout into drawing primitives
    /// </summary>
    /// <param name="logolayout"></param>
    /// <returns>The primitives needed to render this logolayout</returns>
    public IEnumerable<Primitive> ToPrimitives(LogoLayout logolayout)
    {
        var result = new List<Primitive>();

        var logo = logolayout.Logo;

        if (logo is null || ! getLogoAspect.Contains(logo.Path))
        {
            return [];
        }

        var aspect = getLogoAspect.GetAspectRatio(logo.Path);
        var width_factor = (decimal)Math.Sqrt((double)aspect);
        var height_factor = 1.0m / width_factor;
        var icon_width = config.IconSize * width_factor * logo.Scale;
        var icon_height = config.IconSize * height_factor * logo.Scale;
        var imageRect = new Rectangle()
        {
            X = ( logolayout.X - icon_width / 2.0m ) * config.Dpi,
            Y = ( logolayout.Y - icon_height / 2.0m ) * config.Dpi,
            Width = icon_width * config.Dpi,
            Height = icon_height * config.Dpi
        };

        result.Add(
            new ImagePrimitive() { Rectangle = imageRect, Path = logo.Path }
        );

        var text_width_inches = logo.TextWidth ?? config.TextWidth;
        var textRectangle = new Rectangle()
        {
            X = ( logolayout.X - text_width_inches / 2.0m ) * config.Dpi,
            Y = ( logolayout.Y - config.TextHeight / 2.0m + config.TextDistace) * config.Dpi,
            Width = text_width_inches * config.Dpi,
            Height = config.TextHeight * config.Dpi
        };

        result.Add(
            new TextPrimitive() { Rectangle = textRectangle, Text = logo.Title }
        );

        return result;
    }
}