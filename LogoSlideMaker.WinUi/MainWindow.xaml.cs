using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using LogoSlideMaker.Configure;
using LogoSlideMaker.Layout;
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

namespace LogoSlideMaker.WinUi;

/// <summary>
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private Definition? _definition;
    private Layout.Layout? _layout;

    private readonly Dictionary<string, CanvasBitmap> bitmaps = new();

    public MainWindow()
    {
        this.InitializeComponent();
        this.LoadDefinition();

        // TODO: Get the actual system DPI (not just assume 1.5)
        this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((Int32)(1.5*(1280+96*2)), (Int32)(1.5 * (720+64+96*2))));

        this.canvas.CreateResources += Canvas_CreateResources;
    }

    private async void Canvas_CreateResources(Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender, Microsoft.Graphics.Canvas.UI.CanvasCreateResourcesEventArgs args)
    {
        foreach (var file in _definition.Logos.Select(x=>x.Value.Path).Concat(_definition.Files.Template.Bitmaps))
        {
            // We can only load PNGs right now
            if (!bitmaps.ContainsKey(file) )
            {
                var cb = await LoadBitmap(sender, file);
                bitmaps[file] = cb;
            }
        }
        canvas.Invalidate();
    }

    private void LoadDefinition()
    {
        var filename = "sample.toml";
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);
        var sr = new StreamReader(stream!);
        var toml = sr.ReadToEnd();
        _definition = Toml.ToModel<Definition>(toml);

        _layout = new Layout.Layout(_definition, new Variant());
        _layout.Populate();
    }

    private async Task LoadDefinitionAsync(StorageFile storageFile)
    {
        using var stream = await storageFile.OpenStreamForReadAsync();

        var sr = new StreamReader(stream);
        var toml = sr.ReadToEnd();
        _definition = Toml.ToModel<Definition>(toml);

        _layout = new Layout.Layout(_definition, new Variant());
        _layout.Populate();

        // TODO: https://microsoft.github.io/Win2D/WinUI2/html/LoadingResourcesOutsideCreateResources.htm
        Canvas_CreateResources(this.canvas, new CanvasCreateResourcesEventArgs( CanvasCreateResourcesReason.NewDevice));
    }

    void CanvasControl_Draw(
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender,
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        var bgRect = new Rect() { X = 96, Y = 96, Width = 1280, Height = 720 };

        // If there is a bitmap template, draw that
        if ((_definition?.Files.Template.Bitmaps.Count ?? 0) > 0)
        {
            var bitmap = bitmaps.GetValueOrDefault(_definition.Files.Template.Bitmaps[0]);
            if (bitmap is not null)
            {
                args.DrawingSession.DrawImage(bitmap, bgRect);
            }
        }
        else
        {
            // Else Draw a white background
            args.DrawingSession.FillRectangle(bgRect, Microsoft.UI.Colors.White);
        }

        // Render each logo in the layout

        if (_definition is null)
        {
            return;        
        }

        var config = _definition.Render;

        // Draw bounding boxes for any boxes with explicit outer dimensions
        foreach (var box in _definition.Boxes) 
        {
            if (box.Outer != null)
            {
                var boxRect = new Rect() { X = (double)(box.Outer.X * config.Dpi + 96), Y = (double)(box.Outer.Y * config.Dpi + 96), Width = (double)(box.Outer.Width * config.Dpi), Height = (double)(box.Outer.Height * config.Dpi) };
                args.DrawingSession.DrawRectangle(boxRect, Microsoft.UI.Colors.Purple, 1);
            }
        }

        foreach (var boxlayout in _layout)
        {
            foreach (var logolayout in boxlayout.Logos)
            {
                var logo = logolayout.Logo;

                if (logo is null)
                {
                    continue;
                }

                // TODO: DRY refactor this versus powerpoint rendering, which has a lot of
                // overlap, but not 100%

                // Adjust size of icon depending on size of source image. The idea is all
                // icons occupy the same number of pixel area

                var aspect = 1.0m; // pic.Width / pic.Height;
                var width_factor = (decimal)Math.Sqrt((double)aspect);
                var height_factor = 1.0m / width_factor;
                var icon_width = config.IconSize * width_factor * logo.Scale;
                var icon_height = config.IconSize * height_factor * logo.Scale;

                var icon_rect = new Rect();
                icon_rect.X = (float)((logolayout.X - icon_width / 2.0m) * config.Dpi + 96);
                icon_rect.Y = (float)((logolayout.Y - icon_height / 2.0m) * config.Dpi + 96);
                icon_rect.Width = (float)(icon_width * config.Dpi);
                icon_rect.Height = (float)(icon_height * config.Dpi);

                // Draw a logo bounding box
                args.DrawingSession.DrawRectangle(icon_rect, Microsoft.UI.Colors.Red, 1);

                // Draw the actual logo
                var bitmap = bitmaps.GetValueOrDefault(logolayout.Logo.Path);
                if (bitmap is not null)
                {
                    args.DrawingSession.DrawImage(bitmap, icon_rect);
                }

                var text_width_inches = logo.TextWidth ?? config.TextWidth;

                var text_x = (logolayout.X - text_width_inches / 2.0m) * config.Dpi;
                var text_y = (logolayout.Y - config.TextHeight / 2.0m + config.TextDistace) * config.Dpi;
                var text_width = text_width_inches * config.Dpi;
                var text_height = config.TextHeight * config.Dpi;

                //target.AddRectangle(100, 100, 100, 100);
                //var shape = target.Last();

                var shape = new Rect();
                shape.X = 96.0f + (float)text_x;
                shape.Y = 96.0f + (float)text_y;
                shape.Width = (float)text_width;
                shape.Height = (float)text_height;

                // Draw a text bounding box
                args.DrawingSession.DrawRectangle(shape, Microsoft.UI.Colors.Blue, 1);

                // Draw the actual text
                var tf = new CanvasTextFormat() { FontSize = config.FontSize * 96.0f / 72.0f, FontFamily = config.FontName, VerticalAlignment = CanvasVerticalAlignment.Center, HorizontalAlignment = CanvasHorizontalAlignment.Center };
                args.DrawingSession.DrawText(logo.Title, shape, new CanvasSolidColorBrush(sender,Microsoft.UI.Colors.Black), tf);
            }
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

    private void DoExport_Click(object sender, RoutedEventArgs e)
    {
        // Will save this out as powerpoint file
    }

    private void CommandBar_Closing(object sender, object e)
    {
        sender.As<CommandBar>().IsOpen = true;
    }
}
