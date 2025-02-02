using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace LogoSlideMaker.WinUi.Pickers;

public partial class FileOpenPicker(FileOpenPickerViewModel viewModel, Window parent, ILogger logger): IFilePicker
{
    private readonly Windows.Storage.Pickers.FileOpenPicker _picker = new ()
    {
        ViewMode = PickerViewMode.List,
        SuggestedStartLocation = PickerLocationId.DocumentsLibrary,
        SettingsIdentifier = viewModel.SettingsIdentifier
    };

    public async Task Execute()
    {
        try
        {
            foreach (var fileTppe in viewModel.FileTypeFilter)
            {
                _picker.FileTypeFilter.Add(fileTppe);
            }

            // Associate the HWND with the file picker
            var hWnd = WindowNative.GetWindowHandle(parent);
            InitializeWithWindow.Initialize(_picker, hWnd);

            var file = await _picker.PickSingleFileAsync();
            if (file != null)
            {
                // Get off of the UI thread
                _ = Task.Run(() => { viewModel.Continue.Invoke(file.Path); });
                logDebugOk();
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

    [LoggerMessage(Level = LogLevel.Debug, EventId = 3111, Message = "{Location}: OK")]
    public partial void logDebugOk([CallerMemberName] string? location = "");
    
    [LoggerMessage(Level = LogLevel.Debug, EventId = 3114, Message = "{Location}: No file chosen")]
    public partial void logDebugNoFile([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 3118, Message = "{Location}: Failed")]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = "");
}

public interface IFilePicker
{
    Task Execute();
}