using LogoSlideMaker.Models;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.Public;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogoSlideMaker.WinUi.Services;

/// <summary>
/// Renders LogoSlideMaker primitives to a Win2D CanvasControl
/// </summary>
internal class DisplayRenderer(CanvasControl canvas, BitmapCache bitmapCache, ILogger _logger)
{
    // Cached canvas resources
    private ICanvasBrush? solidBlack;

    private readonly Dictionary<TextSyle, CanvasTextFormat> textFormats = new();

    public async Task CreateResourcesAsync(IDefinition definition)
    {
        if (definition.Variants.Count > 0)
        {
            foreach (var format in definition.Variants[0].TextStyles)
            {
                textFormats[format.Key] = new CanvasTextFormat
                {
                    FontSize = (float)format.Value.FontSize * 96.0f / 72.0f,
                    FontFamily = format.Value.FontName,
                    VerticalAlignment = CanvasVerticalAlignment.Center,
                    HorizontalAlignment = CanvasHorizontalAlignment.Center
                };
            }

        }
        solidBlack = new CanvasSolidColorBrush(canvas, Microsoft.UI.Colors.Black);

        // Load (and measure) all the bitmaps
        // NOTE: If multiple TOML files share the same path, we will re-use the previously
        // created canvas bitmap. This could be a problem if two different TOMLs are in 
        // different directories, and use the same relative path to refer to two different
        // images.
        await bitmapCache.LoadAsync(canvas, definition.ImagePaths);
    }

    public void Render(IEnumerable<Primitive> primitives, CanvasDrawingSession session)
    {
        foreach (var p in primitives)
        {
            Draw(p, session);
        }
    }

    private void Draw(Primitive primitive, CanvasDrawingSession session)
    {
        switch (primitive)
        {
            case TextPrimitive text:
                Draw(text, session);
                break;

            case ImagePrimitive image:
                Draw(image, session);
                break;

            case RectanglePrimitive rect:
                Draw(rect, session);
                break;

            default:
                throw new NotImplementedException();
        }
    }

    private void Draw(TextPrimitive primitive, CanvasDrawingSession session)
    {
        if (!textFormats.TryGetValue(primitive.Style, out var format))
            throw new Exception($"Unexpected text style {primitive.Style}");

        session.DrawText(
            primitive.Text,
            primitive.Rectangle.AsWindowsRect(),
            solidBlack,
            format
        );

#if false
        // Move this into generate primitives??

        // Draw a text bounding box
        if (viewModel.ShowBoundingBoxes)
        {
            session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Blue, 1);
        }
#endif
    }

    private void Draw(ImagePrimitive primitive, CanvasDrawingSession session)
    {
        // Draw the actual logo
        var bitmap = bitmapCache.GetOrDefault(primitive.Path);
        if (bitmap is not null)
        {
            var sourceRect = bitmap.Bounds;
            if (primitive.Crop?.IsValid == true)
            {
                sourceRect.X += sourceRect.Width * (double)primitive.Crop.Left;
                sourceRect.Y += sourceRect.Height * (double)primitive.Crop.Top;
                sourceRect.Width *= 1 - (double)primitive.Crop.Right - (double)primitive.Crop.Left;
                sourceRect.Height *= 1 - (double)primitive.Crop.Top - (double)primitive.Crop.Bottom;
            }
            session.DrawImage(bitmap, primitive.Rectangle.AsWindowsRect(), sourceRect, 1.0f, CanvasImageInterpolation.HighQualityCubic);
        }

#if false
        // Move this into generate primitives??
        // Draw a logo bounding box
        if (viewModel.ShowBoundingBoxes)
        {
            session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Red, 1);
        }
#endif
    }

    private static void Draw(RectanglePrimitive primitive, CanvasDrawingSession session)
    {
        if (primitive.Fill)
        {
            session.FillRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.White);
        }
        else
        {
            session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Purple, 1);
        }
    }
}
