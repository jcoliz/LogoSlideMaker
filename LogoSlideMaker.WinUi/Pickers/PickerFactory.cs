using System;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace LogoSlideMaker.WinUi.Pickers;

/// <summary>
/// Service to create file pickers on demand
/// </summary>
/// <param name="window">Parent window to associate the picker with</param>
/// <param name="loggerFactory">How to generate loggers</param>
public class PickerFactory(Lazy<Window> window, ILoggerFactory loggerFactory)
{
    /// <summary>
    /// Create a picker using the supplied view model. Picker type is based on underlying type of the view model.
    /// </summary>
    /// <param name="viewModel"></param>
    /// <returns></returns>
    public IFilePicker? CreatePicker(IPickerViewModel viewModel) => viewModel switch
    {
        FileOpenPickerViewModel fileOpenPickerViewModel => new FileOpenPicker(fileOpenPickerViewModel, window.Value, loggerFactory.CreateLogger<FileOpenPicker>()),
        FileSavePickerViewModel fileSavePickerViewModel => new FileSavePicker(fileSavePickerViewModel, window.Value, loggerFactory.CreateLogger<FileSavePicker>()),
        _ => null
    };
}
