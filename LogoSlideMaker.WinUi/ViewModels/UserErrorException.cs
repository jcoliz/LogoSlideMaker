using System;

namespace LogoSlideMaker.WinUi.ViewModels;

public class UserErrorException(string title, string details): Exception(message: $"{title}: {details}")
{
    public string Title => title;
    public string Details => details;
}
