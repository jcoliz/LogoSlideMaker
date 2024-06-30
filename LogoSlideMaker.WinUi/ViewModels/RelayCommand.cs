using System;
using System.Diagnostics;
using System.Windows.Input;

namespace LogoSlideMaker.WinUi.ViewModels;

/// <summary>
/// Encapsulate arbitrary viewmodel commands into an ICommand for view
/// </summary>
/// <seealso href="https://learn.microsoft.com/en-us/archive/msdn-magazine/2009/february/patterns-wpf-apps-with-the-model-view-viewmodel-design-pattern"/>
public class RelayCommand(Action<object?> execute, Predicate<object?>? canExecute) : ICommand
{
    #region Fields 
    private readonly Action<object?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Predicate<object?>? _canExecute = canExecute;
    #endregion

    #region Constructors 
    public RelayCommand(Action<object?> execute) : this(execute, null) { }
    #endregion

    #region ICommand Members 
    [DebuggerStepThrough]
    public bool CanExecute(object? parameter)
    {
        return _canExecute == null || _canExecute(parameter);
    }
    public event EventHandler? CanExecuteChanged = delegate { };

    public void Execute(object? parameter)
    {
        _execute(parameter);
    }
    #endregion // ICommand Members 
}