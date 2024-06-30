using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using WinRT;

namespace LogoSlideMaker.WinUi;

/// <summary>
/// Logo slide layout previewer
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;

    private CanvasTextFormat? defaultTextFormat;
    private ICanvasBrush? solidBlack;

    private readonly BitmapCache bitmapCache = new();

    public MainWindow()
    {
        viewModel = new(bitmapCache)
        {
            UIAction = x => this.DispatcherQueue.TryEnqueue(() => x())
        };

        viewModel.PropertyChanged += ViewModel_PropertyChanged;

        this.InitializeComponent();
        this.AppWindow.SetIcon("Assets/app-icon.ico");

        this.Root.DataContext = viewModel;

        var dpi = GetDpiForWindow(hWnd);
        this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(dpi*1280/96, dpi*(720+64)/96));

        this.canvas.CreateResources += Canvas_CreateResources;

        this.LoadDefinition_Embedded();
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsLoading))
        {
            if (viewModel.IsLoading)
            {
                // When starting loading, canvas needs to redraw to blank
                canvas.Invalidate();
            }
            else
            {
                // When finished loading, need to load resources back in

                // TODO: https://microsoft.github.io/Win2D/WinUI2/html/LoadingResourcesOutsideCreateResources.htm
                Canvas_CreateResources(this.canvas, new CanvasCreateResourcesEventArgs(CanvasCreateResourcesReason.NewDevice));
            }
        }
    }

    [LibraryImport("User32.dll", SetLastError = true)]
    private static partial int GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Get the current window's HWND by passing in the Window object
    /// </summary>
    private IntPtr hWnd => WinRT.Interop.WindowNative.GetWindowHandle(this);

    private async void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Create some static resource we'll use as part of drawing
        // TODO: Probably should be in viewmodel
        var config = viewModel.RenderConfig;
        if (config is null)
        {
            Trace.WriteLine("No config, skipping");
            return;
        }
        defaultTextFormat = new() { FontSize = config.FontSize * 96.0f / 72.0f, FontFamily = config.FontName, VerticalAlignment = CanvasVerticalAlignment.Center, HorizontalAlignment = CanvasHorizontalAlignment.Center };
        solidBlack = new CanvasSolidColorBrush(sender, Microsoft.UI.Colors.Black);

        // Load (and measure) all the bitmaps
        // NOTE: If multiple TOML files share the same path, we will re-use the previously
        // created canvas bitmap. This could be a problem if two different TOMLs are in 
        // different directories, and use the same relative path to refer to two different
        // images.
        await bitmapCache.LoadAsync(sender, viewModel.ImagePaths);

        // Now that all the bitmaps are loaded, we now have enough information to
        // generate the drawing primitives so we can render them.
        viewModel.GeneratePrimitives();

        // All has changed, redraw!
        canvas.Invalidate();
    }

    private async void LoadDefinition_Embedded()
    {
        var filename = "sample.toml";
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource)!;

        bitmapCache.BaseDirectory = null;

        await Task.Run(() => { viewModel.LoadDefinition(stream); });
    }

    private void CanvasControl_Draw(
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasControl sender,
        Microsoft.Graphics.Canvas.UI.Xaml.CanvasDrawEventArgs args)
    {
        foreach (var p in viewModel.Primitives)
        {
            Draw(p, args.DrawingSession);
        }
    }

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new Windows.Storage.Pickers.FileOpenPicker();

        // https://github.com/microsoft/WindowsAppSDK/issues/1188
        // Associate the HWND with the file picker
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        picker.ViewMode = Windows.Storage.Pickers.PickerViewMode.List;
        picker.SuggestedStartLocation = Windows.Storage.Pickers.PickerLocationId.DocumentsLibrary;
        picker.FileTypeFilter.Add(".toml");

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            var path = file.Path;
            bitmapCache.BaseDirectory = Path.GetDirectoryName(path);
            await viewModel.LoadDefinitionAsync(path);
        }
    }

    private void DoExport_Click(object sender, RoutedEventArgs e)
    {
        // Will save this out as powerpoint file
        // Get the default output file from the definition
        // Bring up a save picker to let user have ultimate decision on file
        // Render to slide
        // Give user option to launch the ppt (would be nice)
    }

    private void PickView_Click(object sender, RoutedEventArgs e)
    {
        // Will let us choose which variant to display
        // Note that this will not require reloading resources, just invalidate the canvas and redraw

        // What I'd LIKE to do is have a menu dynamically created based on the slides (variants)
        // available. Sadly tht's not possible
        // https://github.com/microsoft/microsoft-ui-xaml/issues/1087

        // So, what I'll ultimately do is need to bring up a picker dialog.
        // For now, we'll simply cycle through the available slides

        var current = viewModel.SlideNumber++;
        if (viewModel.SlideNumber == current)
        {
            viewModel.SlideNumber = 0;
        }
        canvas.Invalidate();
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
#if false
        // Draw a text bounding box
        session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Blue, 1);
#endif
        // Draw the actual text
        session.DrawText(primitive.Text, primitive.Rectangle.AsWindowsRect(), solidBlack, defaultTextFormat);
    }

    private void Draw(ImagePrimitive primitive, CanvasDrawingSession session)
    {
#if false
        // Draw a logo bounding box
        session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Red, 1);
#endif
        // Draw the actual logo
        var bitmap = bitmapCache.GetOrDefault(primitive.Path);
        if (bitmap is not null)
        {
            session.DrawImage(bitmap, primitive.Rectangle.AsWindowsRect());
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
}

internal static class Converters
{
    internal static Windows.Foundation.Rect AsWindowsRect(this Configure.Rectangle source)
    {
        return new Rect() { X = (double)source.X, Y = (double)(source.Y ?? 0), Width = (double)source.Width, Height = (double)(source.Height ?? 0)};
    }
}
