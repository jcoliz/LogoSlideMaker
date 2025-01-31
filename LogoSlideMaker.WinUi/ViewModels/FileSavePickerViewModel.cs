using System;
using System.Collections.Generic;

namespace LogoSlideMaker.WinUi.ViewModels;

public record FileSavePickerViewModel
{
    public string SuggestedFileName { get; init; } = string.Empty;
    public string SettingsIdentifier { get; init; } = string.Empty;
    public IReadOnlyDictionary<string, IList<string>> FileTypeChoices { get; init; } = new Dictionary<string, IList<string>>();
    public Action<string> Continue { get; init; } = _ => { };
}
