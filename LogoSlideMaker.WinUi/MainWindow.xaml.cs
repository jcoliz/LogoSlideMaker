using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.LogoSlideMaker_WinUi_XamlTypeInfo;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Svg;
using Tomlyn;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using WinRT;
using static System.Net.Mime.MediaTypeNames;
using Size = LogoSlideMaker.Configure.Size;

namespace LogoSlideMaker.WinUi;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window, IGetImageSize
{
    private Definition? _definition;
    private Layout.Layout? _layout;
    private string? currentFile;
    private CanvasTextFormat? tf;
    private ICanvasBrush? solidBlack;

    private readonly Dictionary<string, CanvasBitmap> bitmaps = new();
    private readonly Dictionary<string, Size> bitmapSizes = new();
    private readonly List<Primitive> _primitives = new();

    Size IGetImageSize.GetSize(string imagePath)
    {
        return bitmapSizes.GetValueOrDefault(imagePath) ?? throw new KeyNotFoundException();
    }

    public MainWindow()
    {
        this.InitializeComponent();
        this.LoadDefinition_Embedded();

        // TODO: Get the actual system DPI (not just assume 1.5)
        this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((Int32)(1.5*(1280+96*2)), (Int32)(1.5 * (720+64+96*2))));

        this.canvas.CreateResources += Canvas_CreateResources;
    }

    private async void Canvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        tf = new() { FontSize = _definition.Render.FontSize * 96.0f / 72.0f, FontFamily = _definition.Render.FontName, VerticalAlignment = CanvasVerticalAlignment.Center, HorizontalAlignment = CanvasHorizontalAlignment.Center };
        solidBlack = new CanvasSolidColorBrush(sender, Microsoft.UI.Colors.Black);

        foreach (var file in _definition.Logos.Select(x=>x.Value.Path).Concat(_definition.Files.Template.Bitmaps))
        {
            // We can only load PNGs right now
            if (!bitmaps.ContainsKey(file) )
            {
                var cb = await LoadBitmap(sender, file);
                bitmaps[file] = cb;

                var bounds = cb.GetBounds(sender);
                bitmapSizes[file] = new Size() { Width = (decimal)bounds.Width, Height = (decimal)bounds.Height };
            }
        }
        canvas.Invalidate();
    }

    private void LoadDefinition_Embedded()
    {
        var filename = "sample.toml";
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);

        LoadDefinition_Local(stream!);
    }

    private async Task LoadDefinitionAsync(StorageFile storageFile)
    {
        currentFile = storageFile.Path;

        using var stream = await storageFile.OpenStreamForReadAsync();

        LoadDefinition_Local(stream);

        // TODO: https://microsoft.github.io/Win2D/WinUI2/html/LoadingResourcesOutsideCreateResources.htm
        Canvas_CreateResources(this.canvas, new CanvasCreateResourcesEventArgs( CanvasCreateResourcesReason.NewDevice));
    }

    private void LoadDefinition_Local(Stream stream)
    {
        var sr = new StreamReader(stream);
        var toml = sr.ReadToEnd();
        _definition = Toml.ToModel<Definition>(toml);

        _layout = new Layout.Layout(_definition, new Variant());
        _layout.Populate();

        GeneratePrimitives();
    }

    /// <summary>
    /// Generate and retain all primitives needed to display this slide
    /// </summary>
    private void GeneratePrimitives()
    {
        _primitives.Clear();
        var config = _definition.Render;

        // Add primitives for a background
        var bgRect = new Configure.Rectangle() { Width = 1280, Height = 720 };

        // If there is a bitmap template, draw that
        var definedBitmaps = _definition?.Files.Template.Bitmaps;
        if (definedBitmaps is not null && definedBitmaps.Count > 0 && bitmaps.ContainsKey(definedBitmaps[0]))
        {
            _primitives.Add(new ImagePrimitive()
            {
                Rectangle = bgRect,
                Path = definedBitmaps[0]
            });
        }
        else
        {
            // Else Draw a white background
            _primitives.Add(new RectanglePrimitive()
            {
                Rectangle = bgRect,
                Fill = true
            });
        }

        // Add needed primitives for each logo
        var generator = new GeneratePrimitives(config, this);
        _primitives.AddRange(_layout.SelectMany(x => x.Logos).SelectMany(generator.ToPrimitives));

        // Add bounding boxes for any boxes with explicit outer dimensions
        _primitives.AddRange(
            _definition.Boxes
                .Where(x => x.Outer is not null)
                .Select(x => new RectanglePrimitive() 
                { 
                    Rectangle = x.Outer
                } 
            )
        );
    }

    private async Task ReloadAsync()
    {
        if (currentFile is null)
        {
            return;        
        }

        var storageFile = await StorageFile.GetFileFromPathAsync(currentFile);

        await LoadDefinitionAsync(storageFile);
    }

    void CanvasControl_Draw(
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender,
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        foreach (var p in _primitives)
        {
            Draw(p, args.DrawingSession);
        }
    }

    /// <summary>
    /// Load a single bitmap from embedded storage
    /// </summary>
    /// <param name="resourceCreator">Where to create bitmaps</param>
    /// <param name="filename">Name of source file</param>
    /// <returns>Created bitmap in this canvas</returns>
    private async Task<CanvasBitmap> LoadBitmap(ICanvasResourceCreator resourceCreator, string filename)
    {
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);

        if (filename.ToLowerInvariant().EndsWith(".svg"))
        {
            var svg = SvgDocument.Open<SvgDocument>(stream);
            var bitmap = svg.Draw();
            var pngStream = new MemoryStream();
            bitmap.Save(pngStream, System.Drawing.Imaging.ImageFormat.Png);
            pngStream.Seek(0, SeekOrigin.Begin);
            var randomAccessStream = pngStream.AsRandomAccessStream();
            var result = await CanvasBitmap.LoadAsync(resourceCreator, randomAccessStream);

            return result;
        }
        else
        {
            var randomAccessStream = stream.AsRandomAccessStream();
            var result = await CanvasBitmap.LoadAsync(resourceCreator, randomAccessStream);

            return result;
        }
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();

        // https://github.com/microsoft/WindowsAppSDK/issues/1188
        // Get the current window's HWND by passing in the Window object
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        // Associate the HWND with the file picker
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".toml");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            await LoadDefinitionAsync(file);
        }
    }

    private async void Reload_Click(object sender, RoutedEventArgs e)
    {
        await ReloadAsync();
    }

    private void DoExport_Click(object sender, RoutedEventArgs e)
    {
        // Will save this out as powerpoint file
    }

    private void PickView_Click(object sender, RoutedEventArgs e)
    {
        // Will let us choose which variant to display
        // Note that this will not require reloading resources, just invalidate the canvas and redraw
    }

    private void CommandBar_Closing(object sender, object e)
    {
        sender.As<CommandBar>().IsOpen = true;
    }

    private void Draw(Primitive primitive, CanvasDrawingSession session)
    {
        switch (primitive)
        {
            case TextPrimitive text:
                Draw(text,session);
                break;

            case ImagePrimitive image:
                Draw(image,session);
                break;

            case RectanglePrimitive rect:
                Draw(rect,session);
                break;

            default:
                throw new NotImplementedException();
        }
    }
    private void Draw(TextPrimitive primitive, CanvasDrawingSession session)
    {
        // Draw a text bounding box
        session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Blue, 1);

        // Draw the actual text
        session.DrawText(primitive.Text, primitive.Rectangle.AsWindowsRect(), solidBlack, tf);
    }

    private void Draw(ImagePrimitive primitive, CanvasDrawingSession session)
    {
        // Draw a logo bounding box
        session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Red, 1);

        // Draw the actual logo
        var bitmap = bitmaps.GetValueOrDefault(primitive.Path);
        if (bitmap is not null)
        {
            session.DrawImage(bitmap, primitive.Rectangle.AsWindowsRect());
        }
    }

    private void Draw(RectanglePrimitive primitive, CanvasDrawingSession session)
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

internal static class Converters
{
    internal static Windows.Foundation.Rect AsWindowsRect(this Configure.Rectangle source)
    {
        return new Rect() { X = (double)source.X, Y = (double)source.Y, Width = (double)source.Width, Height = (double)source.Height };
    }

}
