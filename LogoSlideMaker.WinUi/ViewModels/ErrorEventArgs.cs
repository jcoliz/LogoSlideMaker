using System;

namespace LogoSlideMaker.WinUi.ViewModels;

/// <summary>
/// Event data describing an error which will be shown to the user
/// </summary>
/// <remarks>
/// Generally, we should try to use these for errors that the user caused
/// </remarks>
public class UserErrorEventArgs: EventArgs
{
    /// <summary>
    /// Simple summary of the problem, 12 words max
    /// </summary>
    public string Title
    {
        get;
        init;
    } 
    = string.Empty;

    /// <summary>
    /// Detailed information about the problem, 50 words max
    /// </summary>
    public string Details
    {
        get; 
        init; 
    }
    = string.Empty;
}
