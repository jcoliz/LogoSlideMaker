using System;
using System.Collections.Generic;

namespace LogoSlideMaker.WinUi.ViewModels;

public record FileOpenPickerViewModel
{
    public string SettingsIdentifier { get; init; } = string.Empty;
    public IReadOnlyList<string> FileTypeFilter { get; init; } = new List<string>();
    public Action<string> Continue { get; init; } = _ => { };
}
