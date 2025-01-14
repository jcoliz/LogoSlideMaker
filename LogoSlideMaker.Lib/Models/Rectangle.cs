namespace LogoSlideMaker.Models;

public record Rectangle : Size
{
    public decimal X { get; set; }
    public decimal? Y { get; set; }
}
