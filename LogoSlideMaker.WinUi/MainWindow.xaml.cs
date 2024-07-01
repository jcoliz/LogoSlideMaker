using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace LogoSlideMaker.WinUi;

/// <summary>
/// Logo slide layout previewer
/// </summary>
public sealed partial class MainWindow : Window
{
    #region Fields

    private readonly MainViewModel viewModel;
    private CanvasTextFormat? defaultTextFormat;
    private ICanvasBrush? solidBlack;
    private readonly BitmapCache bitmapCache;

    #endregion

    #region Constructor

    public MainWindow(MainViewModel _viewModel, BitmapCache _bitmapCache)
    {
        this.InitializeComponent();

        viewModel = _viewModel;
        viewModel.UIAction = x => this.DispatcherQueue.TryEnqueue(() => x());
        viewModel.PropertyChanged += ViewModel_PropertyChanged;
        viewModel.DefinitionLoaded += ViewModel_DefinitionLoaded;
        this.Root.DataContext = viewModel;

        this.AppWindow.SetIcon("Assets/app-icon.ico");

        var dpi = GetDpiForWindow(hWnd);
        this.AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(dpi*1280/96, dpi*(720+64)/96));

        bitmapCache = _bitmapCache;
        bitmapCache.BaseDirectory = Path.GetDirectoryName(viewModel.lastOpenedFilePath);
        this.viewModel.ReloadDefinitionAsync().ContinueWith(_ => { });
    }

    #endregion

    #region Event handlers

    private void ViewModel_DefinitionLoaded(object? sender, EventArgs e)
    {
        // TODO: https://microsoft.github.io/Win2D/WinUI2/html/LoadingResourcesOutsideCreateResources.htm
        this.Canvas_CreateResources(this.canvas, new CanvasCreateResourcesEventArgs(CanvasCreateResourcesReason.NewDevice));
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsLoading))
        {
            // When starting loading, canvas needs to redraw to blank
            canvas.Invalidate();
        }

        if (e.PropertyName == nameof(MainViewModel.SlideNumber))
        {
            // New slide, redraw
            canvas.Invalidate();
        }
    }
    #endregion

    #region Command handlers

    private async void OpenFile_Click(object sender, RoutedEventArgs e)
    {
        var picker = new FileOpenPicker()
        {
            ViewMode = PickerViewMode.List,
            SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
            SettingsIdentifier = "Common"
        };
        picker.FileTypeFilter.Add(".toml");

        // https://github.com/microsoft/WindowsAppSDK/issues/1188
        // Associate the HWND with the file picker
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        var file = await picker.PickSingleFileAsync();
        if (file != null)
        {
            var path = file.Path;
            bitmapCache.BaseDirectory = Path.GetDirectoryName(path);
            await viewModel.LoadDefinitionAsync(path);
        }
    }

    private async void DoExport_Click(object sender, RoutedEventArgs e)
    {
        // Will save this out as powerpoint file
        // Get the default output file from the viewmodel
        var path = viewModel.OutputPath;
        if (path is null)
        {
            // Can't save the sample file
            return;        
        }

        // Bring up a save picker to let user have ultimate decision on file
        var picker = new FileSavePicker()
        {
            SuggestedFileName = Path.GetFileName(path),
            DefaultFileExtension = Path.GetExtension(path),
            SettingsIdentifier = "Common"
        };
        picker.FileTypeChoices.Add("PowerPoint Files", [ ".pptx" ] );

        // Associate the HWND with the file picker
        WinRT.Interop.InitializeWithWindow.Initialize(picker, hWnd);

        var file = await picker.PickSaveFileAsync();
        if (file != null)
        {
            // Render to slide
            var outPath = file.Path;

            // TODO: Loading affordance would be nice
            await viewModel.ExportToAsync(outPath);
        }

        // TODO: Give user option to launch the ppt (would be nice)
    }

    #endregion

    #region Canvas management & drawing

    private async void Canvas_CreateResources(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // Create some static resource we'll use as part of drawing
        // Not in view model because we don't want any UI namespaces in there
        var config = viewModel.RenderConfig;
        if (config is null)
        {
            Trace.WriteLine("No config, skipping");
            return;
        }
        if (!canvas.IsLoaded)
        {
            Trace.WriteLine("Canvas not loaded, skipping");
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

        // Now we are really done loading
        viewModel.IsLoading = false;
    }

    private void CanvasControl_Draw(
        CanvasControl sender,
        CanvasDrawEventArgs args)
    {
        foreach (var p in viewModel.Primitives)
        {
            Draw(p, args.DrawingSession);
        }
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

    #endregion

    #region Windows Internals

    [LibraryImport("User32.dll", SetLastError = true)]
    private static partial int GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Get the current window's HWND by passing in the Window object
    /// </summary>
    private IntPtr hWnd => WindowNative.GetWindowHandle(this);

    #endregion
}

internal static class Converters
{
    internal static Windows.Foundation.Rect AsWindowsRect(this Configure.Rectangle source)
    {
        return new Rect() { X = (double)source.X, Y = (double)(source.Y ?? 0), Width = (double)source.Width, Height = (double)(source.Height ?? 0)};
    }
}
