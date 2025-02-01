using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using WinRT.Interop;

namespace LogoSlideMaker.WinUi.Pickers;

public partial class FileSavePicker(FileSavePickerViewModel viewModel, Window parent, ILogger logger)
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
                var outPath = file.Path;

                // Get off of the UI thread
                _ = Task.Run(() => { viewModel.Continue.Invoke(file.Path); });
                logDebugOkMoment("FileSavePicker");
            }
            else
            {
                logDebugNoFile();
            }
        }
        catch (Exception ex)
        {
            logFailMoment(ex,"FileSavePicker");
        }
    }

    [LoggerMessage(Level = LogLevel.Debug, EventId = 3013, Message = "{Location}: {Moment} OK")]
    public partial void logDebugOkMoment(string moment, [CallerMemberName] string? location = "");
    
    [LoggerMessage(Level = LogLevel.Debug, EventId = 3014, Message = "{Location}: No file chosen")]
    public partial void logDebugNoFile([CallerMemberName] string? location = "");

    [LoggerMessage(Level = LogLevel.Error, EventId = 3018, Message = "{Location}: {Moment} Failed")]
    public partial void logFailMoment(Exception ex, string moment, [CallerMemberName] string? location = "");
}
