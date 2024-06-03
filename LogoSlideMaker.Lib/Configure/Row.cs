namespace LogoSlideMaker.Configure;

/// <summary>
/// A method to specify multiple rows in one declaration
/// </summary>
public record Row
{
    public decimal XPosition { get; set; }
    public decimal YPosition { get; set; }
    public decimal Width { get; set; }
    public int MinColumns { get; set; }
    public List<string> Logos { get; set; } = new List<string>();
    public int NumItems => Math.Max( Logos.Count, MinColumns );
    public decimal Spacing => (NumItems > 1) ? Width / (NumItems - 1) : 0;
}
