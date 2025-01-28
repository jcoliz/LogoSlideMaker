using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Models;

namespace LogoSlideMaker.Primitives;

/// <summary>
/// Generates simple drawing primitives to show a single logolayout
/// </summary>
/// <remarks>
/// These primitives could be drawn by any renderer
/// </remarks>
/// <param name="config">Rendering configuration to covern detailed sizing</param>
/// <param name="getLogoAspect">Provides image size for any given image path</param>
internal class PrimitivesEngine(RenderConfig config, IGetImageAspectRatio getLogoAspect)
{
    /// <summary>
    /// Breakdown a single logolayout into drawing primitives
    /// </summary>
    /// <param name="logolayout"></param>
    /// <returns>The primitives needed to render this logolayout</returns>
    public IEnumerable<Primitive> ToPrimitives(LogoLayout logolayout)
    {
        var result = new List<Primitive>();

        if (logolayout.Logo is null)
        {
            // Some confusion here. Worth tracking down what I was expecting by
            // having a null in the logolayouts.
            return [];
        }
        var logo = logolayout.Logo; // ?? new Logo() { Title = "None" };

        var exists = logo.Path is not null && getLogoAspect.Contains(logo.Path);
        var aspect = exists ? getLogoAspect.GetAspectRatio(logo.Path!) : 1m;

        // Currently only right-side cropping supported
        if (logo.Crop?.Right > 0)
        {
            aspect *= 1 - logo.Crop.Right;
        }

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

        var corner = logo.CornerRadius.HasValue 
            ? (decimal?)(logo.CornerRadius.Value * config.Dpi * config.IconSize) 
            : null;

        if (exists)
        {
            result.Add(
                new ImagePrimitive() 
                { 
                    Rectangle = imageRect, 
                    Path = logo.Path!, 
                    Crop = logo.Crop,
                    CornerRadius = corner
                }
            );
        }
        else
        {
            result.Add(
                new RectanglePrimitive() { Rectangle = imageRect }
            );
        }

        // Proposed new change:
        // Default text width is the perfect size to fit given the number of colums
        // Logos can set a minimum, as can the whole file
        //var text_width_inches = Math.Max(config.TextWidth, Math.Max(logolayout.DefaultTextWidth.HasValue ? logolayout.DefaultTextWidth.Value - .02m : 0, logo.TextWidth ?? 0));

        // Unfortunately, this makes the old slide look bad. So I will have to think more
        // about how to configure this. With the two-page layout, I want the first page to
        // have a narrower text width than the second page. So I guess this means I need an
        // override text width on the variant

        // logo layout will now get text wifth from variant. This is kind of a hack.
        var text_width_inches = logo.TextWidth ?? logolayout.DefaultTextWidth ?? config.TextWidth;

        var textRectangle = new Rectangle()
        {
            X = ( logolayout.X - text_width_inches / 2.0m ) * config.Dpi,
            Y = ( logolayout.Y - config.TextHeight / 2.0m + config.TextDistace) * config.Dpi,
            Width = text_width_inches * config.Dpi,
            Height = config.TextHeight * config.Dpi
        };

        result.Add(
            new TextPrimitive() { Rectangle = textRectangle, Text = logo.Title, Style = TextSyle.Logo }
        );

        return result;
    }

    public IEnumerable<Primitive> ToPrimitives(TextLayout textLayout)
    {
        if (textLayout.Position.Kind != EdgeKind.Bottom)
        {
            throw new Exception($"Unknown edge kind {textLayout.Position.Kind}");
        }

        if (config.TitleHeight == null)
        {
            return Enumerable.Empty<Primitive>();
        }

        return 
        [ 
            new TextPrimitive()
            {
                Rectangle = new Rectangle()
                {
                    X = textLayout.Position.X * config.Dpi,
                    Y = (textLayout.Position.Y - config.TitleHeight) * config.Dpi,
                    Width = textLayout.Position.Length * config.Dpi,
                    Height = config.TitleHeight * config.Dpi
                },
                Text = textLayout.Text,
                Style = textLayout.TextSyle
            } 
        ];
    }
}