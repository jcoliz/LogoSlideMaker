using Microsoft.Extensions.Logging;
using Microsoft.UI.Xaml;
using System;

namespace LogoSlideMaker.WinUi.Services;

/// <summary>
/// Provides dispatch onto UI thread
/// </summary>
public interface IDispatcher
{
    /// <summary>
    /// Dispatch this action onto the UI thread
    /// </summary>
    /// <param name="action">Action to dispatch on UI thread</param>
    public void Dispatch(Action action);
}

/// <summary>
/// Provides dispatch onto UI thread of a particular window
/// </summary>
internal class Dispatcher(Lazy<Window> window, ILogger<Dispatcher> logger) : IDispatcher
{
    /// <inheritdoc/>
    public void Dispatch(Action action)
    {
        try
        {
            if (!window.Value.DispatcherQueue.TryEnqueue(() => action.Invoke()))
            {
                logger.LogWarning("Dispatch: Unable to add task");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Dispatch: Failed");
        }
    }
}