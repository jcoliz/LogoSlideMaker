namespace LogoSlideMaker.Configure;

/// <summary>
/// A Collection of logos on one visual line
/// </summary>
/// <remarks>
/// This is largely obsolete now. Use Box instead.
/// </remarks>
internal record Row
{
    public decimal XPosition { get; set; }
    public decimal YPosition { get; set; }
    public decimal Width { get; set; }
    public int MinColumns { get; set; }
    public List<string> Logos { get; set; } = new List<string>();
    public int NumItems => Math.Max( Logos.Count, MinColumns );
    public decimal Spacing => (NumItems > 1) ? Width / (NumItems - 1) : 0;
}
