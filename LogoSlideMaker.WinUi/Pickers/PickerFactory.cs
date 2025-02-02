using System;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace LogoSlideMaker.WinUi.Pickers;

public class PickerFactory(Lazy<Window> window, ILoggerFactory loggerFactory)
{
    public IFilePicker? CreatePicker(IPickerViewModel viewModel) => viewModel switch
    {
        FileOpenPickerViewModel fileOpenPickerViewModel => new FileOpenPicker(fileOpenPickerViewModel, window.Value, loggerFactory.CreateLogger<FileOpenPicker>()),
        FileSavePickerViewModel fileSavePickerViewModel => new FileSavePicker(fileSavePickerViewModel, window.Value, loggerFactory.CreateLogger<FileSavePicker>()),
        _ => null
    };
}
