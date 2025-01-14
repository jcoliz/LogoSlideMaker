namespace LogoSlideMaker.Public;

internal readonly record struct PublicTextStyle(decimal size, string font, string color) : ITextStyle
{
    public decimal FontSize => size;

    public string FontName => font;

    public string FontColor => color;
}