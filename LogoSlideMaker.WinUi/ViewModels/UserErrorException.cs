using System;

namespace LogoSlideMaker.WinUi.ViewModels;

/// <summary>
/// User has caused an error which should be reported to them
/// </summary>
/// <param name="title">Short title of the error</param>
/// <param name="details">In-depth information to help them solve the error</param>
public class UserErrorException(string title, string details): Exception(message: $"{title}: {details}")
{
    /// <summary>
    /// Short title of the error
    /// </summary>
    public string Title => title;

    /// <summary>
    /// In-depth information to help them solve the error
    /// </summary>
    public string Details => details;
}
