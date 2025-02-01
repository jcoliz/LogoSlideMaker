using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
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
    private readonly DisplayRenderer renderer;
    private readonly ILogger<MainWindow> logger;

    // Internal state
    private Point? lastPanningPoint;
    #endregion

    #region Constructor

    public MainWindow(MainViewModel _viewModel, DisplayRenderer _renderer, ILogger<MainWindow> _logger)
    {
        viewModel = _viewModel;
        renderer = _renderer;
        logger = _logger;

        try
        {
            InitializeComponent();

            // Set up renderer
            renderer.Canvas = canvas;

            // Set up view model
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

    private void Root_Loaded(object sender, RoutedEventArgs e)
    {
        _ = viewModel.Start();     
    }

    #endregion

    #region Event handlers

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

        LogUserError(title, details);
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

    private async void Command_Open(object _, RoutedEventArgs __)
    {
        try
        {
            var pickerViewModel = viewModel.FileOpenPickerViewModel;
            var picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.List,
                SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
                SettingsIdentifier = pickerViewModel.SettingsIdentifier,
            };
            foreach (var fileTppe in pickerViewModel.FileTypeFilter)
            {
                picker.FileTypeFilter.Add(fileTppe);
            }

            // https://github.com/microsoft/WindowsAppSDK/issues/1188
            // Associate the HWND with the file picker
            InitializeWithWindow.Initialize(picker, hWnd);

            var file = await picker.PickSingleFileAsync();
            if (file != null)
            {
                var path = file.Path;

                _ = Task.Run(() => { pickerViewModel.Continue.Invoke(file.Path); });
                logDebugMoment("Selected");
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

    private async void Command_Export(object _, RoutedEventArgs __)
    {
        var picker = new Pickers.FileSavePicker(viewModel.FileSavePickerViewModel, this, logger);
        await picker.Execute();
    }

    private async void Command_About(object sender, RoutedEventArgs e)
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
    public partial void LogUserError(string title, string details, [CallerMemberName]string? location = "");

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

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2014, Message = "{Location}: No file chosen")]
    public partial void logDebugNoFile([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2016, Message = "{Location}: No action taken, because busy")]
    public partial void logDebugBusy([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Debug, EventId = 2013, Message = "{Location}: {Moment}")]
    public partial void logDebugMoment(string moment, [CallerMemberName] string? location = "");

    #endregion
}

internal static class Converters
{
    internal static Rect AsWindowsRect(this Models.Rectangle source)
    {
        return new Rect() { X = (double)source.X, Y = (double)(source.Y ?? 0), Width = (double)source.Width, Height = (double)(source.Height ?? 0)};
    }
}
