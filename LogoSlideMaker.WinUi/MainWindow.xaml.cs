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
/// An empty window that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class MainWindow : Window
{
    private readonly MainViewModel viewModel;

    private string? currentFile;
    private CanvasTextFormat? tf;
    private ICanvasBrush? solidBlack;

    private readonly BitmapCache bitmapCache = new();

    public MainWindow()
    {
        viewModel = new(bitmapCache);

        this.InitializeComponent();
        this.LoadDefinition_Embedded();

        var dpi = GetDpiForWindow(hWnd);
        this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32((Int32)(dpi*1280/96), (Int32)(dpi*(720+64)/96)));

        this.canvas.CreateResources += Canvas_CreateResources;
    }

    [DllImport("User32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern int GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Get the current window's HWND by passing in the Window object
    /// </summary>
    private IntPtr hWnd => WinRT.Interop.WindowNative.GetWindowHandle(this);

    private async void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        var config = viewModel.RenderConfig;
        tf = new() { FontSize = config.FontSize * 96.0f / 72.0f, FontFamily = config.FontName, VerticalAlignment = CanvasVerticalAlignment.Center, HorizontalAlignment = CanvasHorizontalAlignment.Center };
        solidBlack = new CanvasSolidColorBrush(sender, Microsoft.UI.Colors.Black);

        await bitmapCache.LoadAsync(sender, viewModel.ImagePaths);

        // Now that all the bitmaps are loaded, we now have enough information to
        // generate the drawing primitives so we can render them.
        viewModel.GeneratePrimitives();

        // Much has been updated, redraw!!
        canvas.Invalidate();
    }

    private void LoadDefinition_Embedded()
    {
        var filename = "sample.toml";
        var names = Assembly.GetExecutingAssembly()!.GetManifestResourceNames();
        var resource = names.Where(x => x.Contains($".{filename}")).Single();
        var stream = Assembly.GetExecutingAssembly()!.GetManifestResourceStream(resource);

        bitmapCache.BaseDirectory = null;
        viewModel.LoadDefinition(stream!);
    }

    private async Task LoadDefinitionAsync(StorageFile storageFile)
    {
        currentFile = storageFile.Path;

        using var stream = await storageFile.OpenStreamForReadAsync();

        bitmapCache.BaseDirectory = Path.GetDirectoryName(currentFile);
        viewModel.LoadDefinition(stream);

        // TODO: https://microsoft.github.io/Win2D/WinUI2/html/LoadingResourcesOutsideCreateResources.htm
        Canvas_CreateResources(this.canvas, new CanvasCreateResourcesEventArgs( CanvasCreateResourcesReason.NewDevice));
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
        var bitmap = bitmapCache.GetOrDefault(primitive.Path);
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
