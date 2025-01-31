using LogoSlideMaker.Models;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LogoSlideMaker.WinUi.Services;

/// <summary>
/// Renders IRenderViewModel to a Win2D CanvasControl
/// </summary>
public partial class DisplayRenderer
{
    private readonly IRenderViewModel _viewModel;
    private readonly IDispatcher _dispatcher;
    private readonly BitmapCache _bitmapCache;
    private readonly ILogger _logger;

    private bool needResourceLoad = false;
    private IEnumerable<Primitive> _primitives = [];

    public DisplayRenderer(IRenderViewModel viewModel, IDispatcher dispatcher, BitmapCache bitmapCache, ILogger<DisplayRenderer> logger)
    {
        _viewModel = viewModel;
        _bitmapCache = bitmapCache;
        _dispatcher = dispatcher;
        _logger = logger;

        viewModel.PropertyChanged += ViewModel_PropertyChanged;
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        try
        {
            if (e.PropertyName == nameof(MainViewModel.Variant) && !_viewModel.IsLoading)
            {
                // Don't need to generate primitivs while we're loading. Bitmaps might not be
                // all loaded yet, and we'll catch the change in IsLoading
                _primitives = _viewModel.Variant.GeneratePrimitives(_bitmapCache);

                // New slide, redraw
                Canvas.Invalidate();
            }

            if (e.PropertyName == nameof(MainViewModel.ShowBoundingBoxes))
            {
                // New slide, redraw
                Canvas.Invalidate();
            }

            if (e.PropertyName == nameof(MainViewModel.IsLoading))
            {
                _primitives = _viewModel.Variant.GeneratePrimitives(_bitmapCache);

                // New slide, redraw
                Canvas.Invalidate();
            }

            if (e.PropertyName == nameof(MainViewModel.Definition))
            {
                // Set up bitmap cache
                _bitmapCache.BaseDirectory = Path.GetDirectoryName(_viewModel.LastOpenedFilePath);

                _dispatcher.Dispatch(async () => {
                    await CreateResourcesAsync();
                    Canvas.Invalidate();
                });
            }

        }
        catch (Exception ex)
        {
            logFail(ex);
        }        
    }

    // Cached canvas resources
    private ICanvasBrush? solidBlack;
    private readonly Dictionary<TextSyle, CanvasTextFormat> textFormats = [];

    public CanvasControl Canvas
    {
        private get => _canvas ?? throw new Exception("Canvas not initialized");
        set
        {
            if (_canvas != null)
            {
                _canvas.Draw -= Canvas_DrawCanvas;
                _canvas.CreateResources -= Canvas_CreateResources;
            }
            _canvas = value;
            _canvas.Draw += Canvas_DrawCanvas;
            _canvas.CreateResources += Canvas_CreateResources;

            logDebugOk();
        }
    }
    CanvasControl? _canvas;

    #region Canvas event handlers

    internal void Canvas_DrawCanvas(CanvasControl _, CanvasDrawEventArgs args)
    {
        try
        {
            if (!_viewModel.IsLoading)
            {
                Render(args.DrawingSession);
            }
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
    }

    internal async void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // This is called by the canvas when it's ready for resources. Typically, this shouldn't be needed,
        // as the canvas should be ready for resources before we have them loaded. However, there
        // seem to be cases where the definition is loaded BEFORE the canvas is ready, so this event
        // handler should ensure the resources are loaded anyway.

        if (needResourceLoad)
        {
            await CreateResourcesAsync();
            logDebugOk();
        }
        else
        {
            logDebugNoLoadNeeded();
        }
    }

    #endregion

    private async Task CreateResourcesAsync()
    {
        try
        {
            // If cansas is not loaded, we can't do this!
            if (!Canvas.IsLoaded)
            {
                logDebugCanvasNotLoaded();
                needResourceLoad = true; // resource loading deferred
                return;
            }

            if (_viewModel.Definition.Variants.Count > 0)
            {
                foreach (var format in _viewModel.Definition.Variants[0].TextStyles)
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
            solidBlack = new CanvasSolidColorBrush(Canvas, Microsoft.UI.Colors.Black);

            // Load (and measure) all the bitmaps
            // NOTE: If multiple TOML files share the same path, we will re-use the previously
            // created canvas bitmap. This could be a problem if two different TOMLs are in 
            // different directories, and use the same relative path to refer to two different
            // images.
            await _bitmapCache.LoadAsync(Canvas, _viewModel.Definition.ImagePaths);

            // Now we're done loading!
            _viewModel.IsLoading = false;

            logDebugOk();
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
    }

    private void Render(CanvasDrawingSession session)
    {
        var primitives = _viewModel.ShowBoundingBoxes ?
            _primitives :
            _primitives.Where(x => x.Purpose != PrimitivePurpose.Extents);

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

        // Draw a text bounding box
        if (_viewModel.ShowBoundingBoxes)
        {
            session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Blue, 1);
        }
    }

    private void Draw(ImagePrimitive primitive, CanvasDrawingSession session)
    {
        // Draw the actual logo
        var bitmap = _bitmapCache.GetOrDefault(primitive.Path);
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

        // Draw a logo bounding box
        if (_viewModel.ShowBoundingBoxes)
        {
            session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Red, 1);
        }
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
    [LoggerMessage(Level = LogLevel.Debug, EventId = 1101, Message = "{Location}: OK")]
    public partial void logDebugOk([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 1108, Message = "{Location}: Failed")]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 1113, Message = "{Location}: Skipping, canvas not loaded")]
    public partial void logDebugCanvasNotLoaded([CallerMemberName] string? location = "");


    [LoggerMessage(Level = LogLevel.Debug, EventId = 1115, Message = "{Location}: No resource load needed at this time")]
    public partial void logDebugNoLoadNeeded([CallerMemberName] string? location = "");

}
