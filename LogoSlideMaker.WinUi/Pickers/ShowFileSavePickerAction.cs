using System;
using LogoSlideMaker.WinUi.Services;
using LogoSlideMaker.WinUi.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.Xaml.Interactivity;

namespace LogoSlideMaker.WinUi.Pickers;

/// <summary>
/// An action which will show user a file picker, depending on the <see cref="IPickerViewModel"/> provided.
/// </summary>
public sealed partial class ShowFilePickerAction : DependencyObject, IAction
{
    /// <summary>
    /// Gets or sets the <see cref="IPickerViewModel"/> to be used by the action.
    /// </summary>
    public IPickerViewModel Source
    {
        get => (IPickerViewModel)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    /// <summary>
    /// Identifies the <seealso cref="Source"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source),
        typeof(IPickerViewModel),
        typeof(ShowFilePickerAction),
        new PropertyMetadata(null));

    /// <inheritdoc/>
    public object Execute(object sender, object parameter)
    {
        if (Source != null)
        {
            var app = (App)Application.Current;
            var dispatcher = app.Services.GetRequiredService<IDispatcher>();

            dispatcher.Dispatch(async () => 
            {
                var picker = app.Services.GetRequiredService<PickerFactory>().CreatePicker(Source);
                if (picker is not null)
                {
                    await picker.Execute();
                }
            });
        }
        else
        {
            throw new ArgumentNullException(nameof(Source));
        }

        return true;
    }
}
