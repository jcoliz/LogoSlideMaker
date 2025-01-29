using LogoSlideMaker.Primitives;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;

namespace LogoSlideMaker.WinUi.Services;

/// <summary>
/// Renders LogoSlideMaker primitives to a Win2D CanvasControl
/// </summary>
internal class DisplayRenderer(CanvasControl canvas, BitmapCache bitmapCache, ILogger _logger)
{
    // Cached canvas resources
    private CanvasTextFormat? defaultTextFormat;
    private CanvasTextFormat? titleTextFormat;
    private ICanvasBrush? solidBlack;

    public void CreateResources()
    {
        defaultTextFormat = new()
        {
            FontSize = (float)10 /*logoStyle.FontSize*/ * 96.0f / 72.0f,
            FontFamily = "Segoe UI", //logoStyle.FontName,
            VerticalAlignment = CanvasVerticalAlignment.Center,
            HorizontalAlignment = CanvasHorizontalAlignment.Center
        };

        titleTextFormat = new()
        {
            FontSize = (float)24 /*tytleStyle.FontSize*/ * 96.0f / 72.0f,
            FontFamily = "Segoe UI", //tytleStyle.FontName
            VerticalAlignment = CanvasVerticalAlignment.Center,
            HorizontalAlignment = CanvasHorizontalAlignment.Center
        };
        solidBlack = new CanvasSolidColorBrush(canvas, Microsoft.UI.Colors.Black);
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
        // Draw the actual text
        session.DrawText(
            primitive.Text,
            primitive.Rectangle.AsWindowsRect(),
            solidBlack,
            primitive.Style switch
            {
                Models.TextSyle.Logo => defaultTextFormat,
                Models.TextSyle.BoxTitle => titleTextFormat,
                _ => throw new Exception($"Unexpected text style {primitive.Style}")
            }
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
