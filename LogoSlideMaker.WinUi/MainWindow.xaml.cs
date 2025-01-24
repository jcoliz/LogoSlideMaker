using LogoSlideMaker.Primitives;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.Brushes;
using Microsoft.Graphics.Canvas.Text;
using Microsoft.Graphics.Canvas.UI;
using Microsoft.Graphics.Canvas.UI.Xaml;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
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

    // Injected dependencies
    private readonly MainViewModel viewModel;
    private readonly BitmapCache bitmapCache;
    private readonly ILogger<MainWindow> logger;

    // Cached canvas resources
    private CanvasTextFormat? defaultTextFormat;
    private CanvasTextFormat? titleTextFormat;
    private ICanvasBrush? solidBlack;

    // Internal state
    private bool needResourceLoad = false;
    private Point? lastPanningPoint;
    #endregion

    #region Constructor

    public MainWindow(MainViewModel _viewModel, BitmapCache _bitmapCache, ILogger<MainWindow> _logger)
    {
        viewModel = _viewModel;
        bitmapCache = _bitmapCache;
        logger = _logger;

        try
        {
            InitializeComponent();

            // Set up view model
            viewModel.UIAction = x => DispatcherQueue.TryEnqueue(() => x());
            viewModel.PropertyChanged += ViewModel_PropertyChanged;
            viewModel.DefinitionLoaded += ViewModel_DefinitionLoaded;
            viewModel.ErrorFound += DisplayViewModelError;
            Root.DataContext = viewModel;
            Title = MainViewModel.AppDisplayName;
            Root.Loaded += Root_Loaded;

            // Set up app window
            var dpi = GetDpiForWindow(hWnd);
            AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(dpi * viewModel.PlatenSize.Width / 96, dpi * (viewModel.PlatenSize.Height + (int)commandBar.Height) / 96));
            AppWindow.SetIcon("Assets/app-icon.ico");
            AppWindow.Closing += CloseApp;

            logAppVersion(viewModel.BuildVersion);

            logOk();
        }
        catch (Exception ex)
        {
            logCritical(ex);
        }
    }

    private async void Root_Loaded(object sender, RoutedEventArgs e)
    {
        // Reload last-used definition
        try
        {
            await viewModel.ReloadDefinitionAsync();

            logOkMoment("Reload");
        }
        catch (Exception ex)
        {
            logFail(ex);
        }        
    }

    #endregion

    #region Event handlers

    private void ViewModel_DefinitionLoaded(object? sender, EventArgs e)
    {
        var enqueued = DispatcherQueue.TryEnqueue(() =>
        {
            // Set up bitmap cache
            bitmapCache.BaseDirectory = Path.GetDirectoryName(viewModel.LastOpenedFilePath);

            // TODO: https://microsoft.github.io/Win2D/WinUI2/html/LoadingResourcesOutsideCreateResources.htm
            CreateResources(canvas);
        });

        if (!enqueued)
        {
            logFailMoment("Enqueue Create Resources");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsLoading))
        {
            // During and after loading, canvas needs to redraw to blank
            canvas.Invalidate();
        }

        if (e.PropertyName == nameof(MainViewModel.SlideNumber))
        {
            // New slide, redraw
            canvas.Invalidate();
        }

        if (e.PropertyName == nameof(MainViewModel.ShowBoundingBoxes))
        {
            // New slide, redraw
            canvas.Invalidate();
        }
    }

    private void DisplayViewModelError(object? sender, ViewModels.UserErrorEventArgs e)
    {
        var enqueued = DispatcherQueue.TryEnqueue(() =>
        {
            ShowErrorAsync(e.Title, e.Details).ContinueWith(t =>
            {
                if (t.Exception is not null)
                {
                    logFailMoment(t.Exception, "Displaying user error");
                }
            });
        });

        if (!enqueued)
        {
            logFailMoment(e.Title);
        }
    }

    private async Task ShowErrorAsync(string title, string details)
    {
        var dialog = new ContentDialog
        {
            Title = title,
            Content = details,
            CloseButtonText = "OK",
            XamlRoot = Root.XamlRoot
        };

        var _ = await dialog.ShowAsync();

        logOkTitleDetails(title, details);
    }

    private Task ShowErrorAsync(ViewModels.UserErrorEventArgs eventargs) => ShowErrorAsync(eventargs.Title, eventargs.Details);
    private Task ShowErrorAsync(UserErrorException ex) => ShowErrorAsync(ex.Title, ex.Details);

    private void CreateResourcesEvent(CanvasControl sender, CanvasCreateResourcesEventArgs args)
    {
        // This is called by the canvas when it's ready for resources. Typically, this shouldn't be needed,
        // as the canvas should be ready for resources before we have them loaded. However, there
        // seem to be cases where the definition is loaded BEFORE the canvas is ready, so this event
        // handler should ensure the resources are loaded anyway.

        if (!needResourceLoad)
        {
            // When viewmodel is DONE loading, it will call us
            logDebugNoLoadNeeded();
            return;
        }

        CreateResources(sender);
    }

    private void ScrollViewer_ResetPanning(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        lastPanningPoint = null;
    }

    private void ScrollViewer_PointerMoved(object sender, Microsoft.UI.Xaml.Input.PointerRoutedEventArgs e)
    {
        if (e.Pointer.IsInContact && e.Pointer.PointerDeviceType is Microsoft.UI.Input.PointerDeviceType.Mouse or Microsoft.UI.Input.PointerDeviceType.Touchpad)
        {
            var pt = e.GetCurrentPoint(canvasScrollViewer);
            if (lastPanningPoint.HasValue)
            {
                var deltaX = pt.Position.X - lastPanningPoint.Value.X;
                var deltaY = pt.Position.Y - lastPanningPoint.Value.Y;

                var newX = canvasScrollViewer.HorizontalOffset - deltaX;
                var newY = canvasScrollViewer.VerticalOffset - deltaY;
                canvasScrollViewer.ScrollToHorizontalOffset(newX);
                canvasScrollViewer.ScrollToVerticalOffset(newY);
            }
            lastPanningPoint = pt.Position;
        }
        else
        {
            lastPanningPoint = null;
        }
    }

    private void Window_SizeChanged(object sender, WindowSizeChangedEventArgs args)
    {
        var zoomWidth = args.Size.Width / viewModel.PlatenSize.Width;
        var zoomHeight = (args.Size.Height - commandBar.Height) / viewModel.PlatenSize.Height;
        var zoom = Math.Min(zoomWidth, zoomHeight);
        canvasScrollViewer.ZoomToFactor((float)zoom);
    }

    private void Window_Closed(object _, WindowEventArgs __)
    {
        Application.Current.Exit();
    }

    private void CloseApp(Microsoft.UI.Windowing.AppWindow sender, Microsoft.UI.Windowing.AppWindowClosingEventArgs args)
    {
        logOk();
    }

    #endregion

    #region Command handlers

    private async void OpenDocument(object _, RoutedEventArgs __)
    {
        try
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
            InitializeWithWindow.Initialize(picker, hWnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var path = file.Path;
                logOkMomentPath("Selected", path);

                bitmapCache.BaseDirectory = Path.GetDirectoryName(path);
                await viewModel.LoadDefinitionAsync(path);
            }
            else
            {
                logDebugNoFile();
            }
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
    }

    private async void ExportSlides(object _, RoutedEventArgs __)
    {
        try
        {
            // Bring up a save picker to let user have ultimate decision on file
            var picker = new FileSavePicker()
            {
                SuggestedFileName = Path.GetFileName(viewModel.OutputPath ?? viewModel.LastOpenedFilePath ?? "logo-slides.pptx"),
                SettingsIdentifier = "Common"
            };
            picker.FileTypeChoices.Add("PowerPoint Files", [".pptx"]);

            // Associate the HWND with the file picker
            InitializeWithWindow.Initialize(picker, hWnd);

            var file = await picker.PickSaveFileAsync();
            if (file != null)
            {
                var outPath = file.Path;

                // Get off of the UI thread
                var ___ = Task.Run(async () =>
                {
                    await viewModel.ExportToAsync(outPath);
                })
                .ContinueWith(t => 
                {
                    if (t.Exception is AggregateException aex)
                    {
                        foreach(var ex in aex.InnerExceptions)
                        {
                            if (ex is UserErrorException uex)
                            {
                                // Ok, we need to get BACK on the ui thread to show this error!!
                                DispatcherQueue.TryEnqueue(async () =>
                                {
                                    await ShowErrorAsync(uex);
                                });
                            }
                            else
                            {
                                logFailMoment(ex,"Export aggregate");
                            }
                        }
                    }
                    if (t.Exception is not null)
                    {
                        logFailMoment(t.Exception,"Export single exception");
                    }
                    else
                    {
                        // TODO: This would be the time to emit an event that we have successfully
                        // exported
                        logOkPath(outPath);
                    }
                });
            }
            else
            {
                logDebugNoFile();
            }

            // TODO: Give user option to launch the ppt (would be nice)
        }
        catch (UserErrorException ex)
        {
            // Return user-caused errors to the user
            // Note that this will also log the details
            await ShowErrorAsync(ex);
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
    }

    private async void ShowAboutDialog(object sender, RoutedEventArgs e)
    {
        try
        {
            await aboutDialog.ShowAsync();

            logOk();
        }
        catch (Exception ex)
        {
            const int E_ASYNC_OPERATION_NOT_STARTED = unchecked((int)0x80000019);
            if (ex.HResult == E_ASYNC_OPERATION_NOT_STARTED)
            {
                logDebugBusy();
            }
            else
            {
                logFail(ex);            
            }
        }
    }

    [SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "Windows requires a member method here")]
    [SuppressMessage("CodeQuality", "IDE0079:Remove unnecessary suppression", Justification = "It really is required!")]
    private void OpenLogsFolder(ContentDialog _, ContentDialogButtonClickEventArgs __)
    {
        Process.Start("explorer.exe", MainViewModel.LogsFolder);
        logOk();
    }

    #endregion

    #region Canvas management & drawing

    private async void CreateResources(CanvasControl sender)
    {
        try
        {
            // Create some static resource we'll use as part of drawing
            // Not in view model because we don't want any UI namespaces in there
            if (!canvas.IsLoaded)
            {
                needResourceLoad = true;
                logDebugNotLoaded();
                return;
            }
            needResourceLoad = false;

            // NOTE: This is only called once per LOAD, so we don't currently support changing
            // text styles per variant. Ergo, there is a subtle bug here right now.
            var logoStyle = viewModel.TextStyles[Models.TextSyle.Logo];
            defaultTextFormat = new() 
            { 
                FontSize = (float)logoStyle.FontSize * 96.0f / 72.0f, 
                FontFamily = logoStyle.FontName, 
                VerticalAlignment = CanvasVerticalAlignment.Center, 
                HorizontalAlignment = CanvasHorizontalAlignment.Center 
            };

            var tytleStyle = viewModel.TextStyles[Models.TextSyle.BoxTitle];
            titleTextFormat = new() 
            { 
                FontSize = (float)tytleStyle.FontSize * 96.0f / 72.0f, 
                FontFamily = tytleStyle.FontName, 
                VerticalAlignment = CanvasVerticalAlignment.Center, 
                HorizontalAlignment = CanvasHorizontalAlignment.Center 
            };
            solidBlack = new CanvasSolidColorBrush(sender, Microsoft.UI.Colors.Black);
            logDebugLoading();

            // Load (and measure) all the bitmaps
            // NOTE: If multiple TOML files share the same path, we will re-use the previously
            // created canvas bitmap. This could be a problem if two different TOMLs are in 
            // different directories, and use the same relative path to refer to two different
            // images.
            await bitmapCache.LoadAsync(sender, viewModel.ImagePaths);

            // Now that all the bitmaps are loaded, we now have enough information to
            // generate the drawing primitives so we can render them.
            viewModel.GeneratePrimitives();

            logDebugOk();
        }
        catch (Exception ex)
        {
            logFail(ex);
        }
        finally
        {
            // Now we are really done loading
            viewModel.IsLoading = false;
        }
    }

    private void DrawCanvas(CanvasControl _, CanvasDrawEventArgs args)
    {
        try
        {
            var primitives = viewModel.ShowBoundingBoxes ?
                viewModel.Primitives :
                viewModel.Primitives.Where(x => x.Purpose != PrimitivePurpose.Extents);

            if (primitives is null)
            {
                logFail();
                return;
            }

            foreach (var p in primitives)
            {
                Draw(p, args.DrawingSession);
            }
        }
        catch (Exception ex)
        {
            logFail(ex);
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

        // Draw a text bounding box
        if (viewModel.ShowBoundingBoxes)
        {
            session.DrawRectangle(primitive.Rectangle.AsWindowsRect(), Microsoft.UI.Colors.Blue, 1);
        }
    }

    private void Draw(ImagePrimitive primitive, CanvasDrawingSession session)
    {
        // Draw the actual logo
        var bitmap = bitmapCache.GetOrDefault(primitive.Path);
        if (bitmap is not null)
        {
            session.DrawImage(bitmap, primitive.Rectangle.AsWindowsRect(), bitmap.Bounds, 1.0f, CanvasImageInterpolation.HighQualityCubic);
        }

        // Draw a logo bounding box
        if (viewModel.ShowBoundingBoxes)
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

#endregion

    #region Windows Internals

    [LibraryImport("User32.dll", SetLastError = true)]
    private static partial int GetDpiForWindow(IntPtr hwnd);

    /// <summary>
    /// Get the current window's HWND by passing in the Window object
    /// </summary>
    private IntPtr hWnd => WindowNative.GetWindowHandle(this);

    #endregion

    #region Logging

    [LoggerMessage(Level = LogLevel.Information, EventId = 2000, Message = "{Location}: OK")]
    public partial void logOk([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 2010, Message = "{Location}: OK {Path}")]
    public partial void logOkPath(string path, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 2020, Message = "{Location}: {Moment} OK")]
    public partial void logOkMoment(string moment, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 2030, Message = "{Location}: {Moment} OK {Path}")]
    public partial void logOkMomentPath(string moment, string path, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 2040, Message = "{Location}: OK {Title} {Details}")]
    public partial void logOkTitleDetails(string title, string details, [CallerMemberName]string? location = "");

    [LoggerMessage(Level = LogLevel.Information, EventId = 2002, Message = "{Location}: Version {AppVersion}")]
    public partial void logAppVersion(string appversion, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 2008, Message = "{Location}: Failed")]
    public partial void logFail(Exception ex, [CallerMemberName]string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 2018, Message = "{Location}: Failed")]
    public partial void logFail([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 2028, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(Exception ex, string moment, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 2038, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(string moment, [CallerMemberName]string? location = "");

    [LoggerMessage(Level = LogLevel.Critical, EventId = 2009, Message = "{Location}: Critical failure")]
    public partial void logCritical(Exception ex, [CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2001, Message = "{Location}: OK")]
    public partial void logDebugOk([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2011, Message = "{Location}: Loading...")]
    public partial void logDebugLoading([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2012, Message = "{Location}: Skipping, no render config")]
    public partial void logDebugNoRenderConfig([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2013, Message = "{Location}: Skipping, canvas not loaded")]
    public partial void logDebugNotLoaded([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2014, Message = "{Location}: No file chosen")]
    public partial void logDebugNoFile([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2015, Message = "{Location}: No resource load needed at this time")]
    public partial void logDebugNoLoadNeeded([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2016, Message = "{Location}: No action taken, because busy")]
    public partial void logDebugBusy([CallerMemberName] string? location = "");

    #endregion
}

internal static class Converters
{
    internal static Rect AsWindowsRect(this Models.Rectangle source)
    {
        return new Rect() { X = (double)source.X, Y = (double)(source.Y ?? 0), Width = (double)source.Width, Height = (double)(source.Height ?? 0)};
    }
}
