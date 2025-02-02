using System;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;

namespace LogoSlideMaker.WinUi.Pickers;

public class PickerFactory(Lazy<Window> window, ILoggerFactory loggerFactory)
{
    public T? CreatePicker<T>(IPickerViewModel viewModel) where T : class
    {
        if (typeof(T) == typeof(FileSavePicker) && viewModel is FileSavePickerViewModel fileSavePickerViewModel)
        {
            var picker = new FileSavePicker(fileSavePickerViewModel, window.Value, loggerFactory.CreateLogger<FileSavePicker>());
            return picker as T;
        }

        return null;
    }
}
