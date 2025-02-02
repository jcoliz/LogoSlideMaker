using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinRT.Interop;

namespace LogoSlideMaker.WinUi.Pickers;

/// <summary>
/// Manages a file save picker with connection back to the viewmodel
/// </summary>
/// <param name="viewModel">Details on what to display and what to do with chosen file</param>
/// <param name="parent">Which window should this dialog be parented to</param>
/// <param name="logger">Where to send logs</param>
public partial class FileSavePicker(FileSavePickerViewModel viewModel, Window parent, ILogger logger) : IFilePicker
{
    private readonly Windows.Storage.Pickers.FileSavePicker _picker = new ()
    {
        SuggestedFileName = viewModel.SuggestedFileName,
        SettingsIdentifier = viewModel.SettingsIdentifier
    };

    public async Task Execute()
    {
        try
        {
            foreach (var kvp in viewModel.FileTypeChoices)
            {
                _picker.FileTypeChoices.Add(kvp);
            }

            // Associate the HWND with the file picker
            var hWnd = WindowNative.GetWindowHandle(parent);
            InitializeWithWindow.Initialize(_picker, hWnd);

            var file = await _picker.PickSaveFileAsync();
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

    [LoggerMessage(Level = LogLevel.Debug, EventId = 3011, Message = "{Location}: OK")]
    public partial void logDebugOk([CallerMemberName] string? location = "");
    
    [LoggerMessage(Level = LogLevel.Debug, EventId = 3014, Message = "{Location}: No file chosen")]
    public partial void logDebugNoFile([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 3018, Message = "{Location}: Failed")]
    public partial void logFail(Exception ex, [CallerMemberName] string? location = "");
}
